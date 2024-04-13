<h1>In progress....need to update due to email sending notifications: also add assumptions like language, correct app settings fill, ...</h1>

<h2>Application Purpose</h2>

<p>This console application is designed to streamline the management of work hours for team leaders and managers. It automates the retrieval of data from our HR tool QUAD, that tracks hours worked by employees. The primary aim is to assist those responsible for teams in monitoring the work hours of their direct reports. This is crucial for ensuring that team members are maintaining a healthy balance, not exceeding or falling short of the required work hours.</p>

<p>The tool scrapes data related to the adaptability hours &mdash; the flexibility in work hours that employees have &mdash; for each individual who reports directly to a manager. By automating this process, the application aids in the early detection of potential issues, such as excessive overtime or insufficient work hours, which can affect team performance and individual well-being.</p>

<p>Future enhancements to this application will include the functionality to automatically send email alerts when an employee&#39;s recorded hours surpass or fall below predefined thresholds. This feature aims to facilitate proactive management and support, ensuring that both team leaders and members can address workload balance effectively.</p>

<h2>Setup Instructions</h2>

<p>To ensure the console application functions correctly, it&#39;s essential to configure it with your QUAD login credentials. Follow these steps to set up your environment:</p>

<h3>Configuring Application Settings</h3>

<p>Locate the appsettings.json File: This file contains the necessary configurations for the application to run, including placeholders for your QUAD login credentials.</p>

<p>Fill in Your Credentials: Open appsettings.json and enter your QUAD login credentials in the designated fields. This will allow the application to authenticate and perform the necessary data retrieval operations.</p>

<h3>Development Configuration</h3>

<p>For development purposes, it is recommended to use a separate configuration file to avoid accidentally committing sensitive information to version control.</p>

<p>Create a Development Configuration File: Copy the appsettings.json file and rename the copy to appsettings.Development.json. This file will be used for development settings and should not be committed to your version control system.</p>

<p>Enter Your Development Credentials: Open appsettings.Development.json and fill in your development environment&#39;s QUAD login credentials.</p>

<p>Exclude From Version Control: Ensure that appsettings.Development.json is listed in your .gitignore file to prevent it from being accidentally committed and pushed to your repository.</p>
