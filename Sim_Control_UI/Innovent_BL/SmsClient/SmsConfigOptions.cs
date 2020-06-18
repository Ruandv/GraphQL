namespace Innovent_BL.SmsClient
{
    public class SmsConfigOptions
    {
        public const string SectionDescription = "SmsConfig";
        public string AccountSid { get; set; }
        public string AuthToken { get; set; }
        public string FromNumber { get; set; }
        public string ToNumber { get; set; }
    }
}
