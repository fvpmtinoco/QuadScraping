using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using QuadScraping.Common;
using Microsoft.Playwright;
using System.Net.Mail;
using System.Net;
using System.Xml.Linq;

Dictionary<string, string> data = new Dictionary<string, string>();

//get the url from appsettings.json file
var appSettings = LoadAppSettings();
if (appSettings == null)
{
    Console.WriteLine("AppSettings not found");
    return;
}

using var playwright = await Playwright.CreateAsync();

await using var browser = await playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions { Headless = true });
var page = await browser.NewPageAsync();

// Go to the login page
await page.GotoAsync(appSettings.QuadSettings.LoginUrl);

// Type the username and password and click the login button
await page.FillAsync("input[name='username']", appSettings.QuadSettings.Username);
await page.PressAsync("input[name='username']", "Tab");
await page.FillAsync("input[name='password']", appSettings.QuadSettings.Password);
await page.ClickAsync("#js-login-btn");

//Wait for page after login to load without looking for any component
await page.WaitForTimeoutAsync(5000);

//Wait for page after login to load 
await page.WaitForSelectorAsync("#profile_change", new PageWaitForSelectorOptions { Timeout = 5000 });

await page.SelectOptionAsync("#profile_change", new SelectOptionValue { Value = "2" });


//Wait for dsv page to load - It contais the span with the text "Capital Humano"
await page.WaitForSelectorAsync("a[title='Capital Humano']");
await page.WaitForTimeoutAsync(5000);
//Expand "Gestão tempo"
await page.ClickAsync("a[title='Gestão Tempo']");
await Task.Delay(1000);

var cadastro = await page.WaitForSelectorAsync("a[data-filter-tags='quad-hcm gestão tempo cadastro']", new PageWaitForSelectorOptions { Timeout = 5000 });
await cadastro.ClickAsync();

// Wait for the "Adaptabilidade" tab to be clickable
await page.WaitForSelectorAsync("//label[@class='form-label required' and @for='xt_RHID']");
var adaptabilidadeTab = await page.WaitForSelectorAsync("//a[contains(text(), 'Adaptabilidade')]", new PageWaitForSelectorOptions { State = WaitForSelectorState.Visible });
await adaptabilidadeTab.ClickAsync();

// Wait for the "SALDO" text to be visible, it means the page is loaded
await page.WaitForSelectorAsync("#AdaptabilidadeResume");

// Wait for the "RHID" dropdown to load and become visible
var dropDownPerson = await page.WaitForSelectorAsync("#xt_RHID_chosen");
dropDownPerson.ClickAsync();

await Task.Delay(500);
await page.QuerySelectorAllAsync("#xt_RHID_chosen .chosen-results li");

// Now count the dropdown options
int collaboratorsCount = await page.Locator("#xt_RHID_chosen .chosen-results li").CountAsync();

// Initialize a list to hold the text of each option
List<PersonData> personData = new List<PersonData>();

for (int i = 0; i < collaboratorsCount; i++)
{
    // Get the text content of each option and add it to the list
    // Since the dropdown closes after a selection, reopen it for each iteration)
    // Fetch the current option to interact with
    var person = page.Locator("#xt_RHID_chosen .chosen-results li").Nth(i);

    // Get the text content of the current option and add it to the list
    var personName = await person.TextContentAsync();

    // Click the current option
    await person.ClickAsync();

    await page.WaitForSelectorAsync("#xt_EMPRESA_chosen");
    await page.ClickAsync("#xt_EMPRESA_chosen");
    // Select the first option within xt_EMPRESA_chosen
    await page.ClickAsync("#xt_EMPRESA_chosen .chosen-results li:nth-of-type(1)");

    // Wait for xt_DT_ADMISSAO_chosen to load and then click to open
    await page.WaitForSelectorAsync("#xt_DT_ADMISSAO_chosen");
    await page.ClickAsync("#xt_DT_ADMISSAO_chosen");
    // Select the first option within xt_DT_ADMISSAO_chosen
    await page.ClickAsync("#xt_DT_ADMISSAO_chosen .chosen-results li:nth-of-type(1)");

    //Wait for data to load
    Thread.Sleep(5000);

    // Use a selector that finds a div with class 'subheader-block' that contains the text 'SALDO'
    var saldoSectionSelector = "div.subheader-block:has-text('SALDO')";

    // Wait for the element to be present in the DOM
    var saldoSection = await page.WaitForSelectorAsync(saldoSectionSelector);

    if (saldoSection != null)
    {
        // If the section is found, get the saldo value from the span within the section
        var dynamicValueElement = await saldoSection.QuerySelectorAsync(".fw-500.fs-xl.d-block.color-primary-500");
        if (dynamicValueElement != null)
        {
            var dynamicValueText = await dynamicValueElement.InnerTextAsync();
            dynamicValueText = dynamicValueText.Trim();
            var balance = dynamicValueText.Split(":");
            personData.Add(new PersonData() { Name = personName, Adaptability = new TimeSpan(Convert.ToInt32(balance[0]), Convert.ToInt32(balance[1]), 0) });
        }
    }

    // Open the dropdown again for the next iteration
    if (i < collaboratorsCount - 1)
        dropDownPerson.ClickAsync();
}

//Close the browser
await browser.CloseAsync();

//Create a new mail message
using var mail = new MailMessage();
mail.From = new MailAddress(appSettings.EMailSettings.Credentials.Email);
if (appSettings.EMailSettings.CCEmails != null)
    appSettings.EMailSettings.CCEmails.ForEach(cc => mail.CC.Add(cc));

var eligibleNotifications = personData.Where(p => p.Adaptability.TotalHours >= appSettings.EMailSettings.PositiveThreshold || p.Adaptability.TotalHours < appSettings.EMailSettings.NegativeThreshold).ToList();

foreach (var p in eligibleNotifications)
{
    mail.To.Clear();

    var person = SplitPersonData(p.Name);
    if (person.shortName == string.Empty || appSettings.EmailMappings.Mappings.Single(a => a.Id == person.id) == null)
        continue;

    var mapping = appSettings.EmailMappings.Mappings.Single(a => a.Id == person.id);

    mail.To.Add(mapping.Email);
    mail.Subject = p.Adaptability.TotalHours >= appSettings.EMailSettings.PositiveThreshold ? appSettings.EMailSettings.SubjectPositiveAdaptability : appSettings.EMailSettings.SubjectNegativeAdaptability;
    mail.Body = (p.Adaptability.TotalHours >= appSettings.EMailSettings.PositiveThreshold ? appSettings.EMailSettings.BodyEmailPositiveAdaptability : appSettings.EMailSettings.BodyEmailNegativeAdaptability)
                .Replace("@name", person.shortName)
                .Replace("@balanceHours", ((int)p.Adaptability.TotalHours).ToString())
                .Replace("@balanceMinutes", ((int)p.Adaptability.Minutes).ToString());

    // Send the email
    using (var smtp = new SmtpClient("smtp.office365.com", 587))
    {
        smtp.Credentials = new NetworkCredential(appSettings.EMailSettings.Credentials.Email, appSettings.EMailSettings.Credentials.Pass);
        smtp.EnableSsl = true;
        await smtp.SendMailAsync(mail);
    }
}

static AppSettings? LoadAppSettings()
{
    var settings = new ConfigurationBuilder()
        .Add(new JsonConfigurationSource { Path = "appsettings.json", Optional = false, ReloadOnChange = true })
        .Add(new JsonConfigurationSource { Path = "appsettings.Development.json", Optional = true, ReloadOnChange = true });

    var config = settings.Build();

    return config.Get<AppSettings>();
}

static (int id, string shortName) SplitPersonData(string input)
{
    if (string.IsNullOrEmpty(input) || !input.Contains('-'))
    {
        return (0, string.Empty);
    }

    // Split the string on the '-' character
    string[] parts = input.Split('-');

    // The second part should be the name
    string name = parts[1].Trim();

    // Split the name into words
    string[] words = name.Split(' ');

    // The first and last words should be the first and last names
    string firstName = words[0];
    string lastName = words[words.Length - 1];
    return (int.Parse(parts[0].Trim()), $"{firstName} {lastName}");
}

class PersonData
{
    public string Name { get; set; }
    public TimeSpan Adaptability { get; set; }
}