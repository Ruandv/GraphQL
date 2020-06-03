﻿namespace Innovent_SimControl_Service
{
    public class InnoventSettingOptions
    {
        public const string SectionDescription = "Innovent_Settings";

        public string[] ContactNumbers { get; set; }
        public string ApiKey { get; set; }
        public string GraphUrl { get; set; }
        public string MinimumLimit { get; set; }
        public int Delay { get; set; }
    }
}
