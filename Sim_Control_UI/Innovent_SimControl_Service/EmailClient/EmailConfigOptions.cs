using System.Collections.Generic;

namespace Innovent_SimControl_Service
{

    public class EmailConfigOptions
    {
        public const string SectionDescription = "EmailConfig";
        public string From { get; set; }
        public List<string> AdministratorEmails { get; set; }
        public string SmtpServer { get; set; }
        public int Port { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
    }
}
