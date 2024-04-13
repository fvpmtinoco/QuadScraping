namespace QuadScraping.Common
{
    public record AppSettings
    {
        public QuadSettings QuadSettings { get; set; } = new QuadSettings();
        public EmailSettings EMailSettings { get; set; } = new EmailSettings();
        public EmailMappings EmailMappings { get; set; } = new EmailMappings();
    }

    public class QuadSettings
    {
        public string? LoginUrl { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }
    }

    public class EmailSettings
    {
        public int PositiveThreshold { get; set; } = 8;
        public int NegativeThreshold { get; set; } = -4;
        public string SubjectPositiveAdaptability { get; set; }
        public string BodyEmailPositiveAdaptability { get; set; }
        public string SubjectNegativeAdaptability { get; set; }
        public string BodyEmailNegativeAdaptability { get; set; }
        public List<string>? CCEmails { get; set; }
        public Credentials Credentials { get; set; }
    }

    public class Credentials
    {
        public string Email { get; set; }
        public string Pass { get; set; }
    }

    public class EmailMappings
    {
        public List<Mappings> Mappings { get; set; }
    }

    public class Mappings
    {
        public int Id { get; set; }
        public string Email { get; set; }
    }
}
