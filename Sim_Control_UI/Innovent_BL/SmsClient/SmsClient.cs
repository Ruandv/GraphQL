using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Innovent_BL.SmsClient
{
    public interface ISmsClient
    {
        void Send(string msg);

    }
    public class SmsClient : ISmsClient
    {
        private IOptions<SmsConfigOptions> _smsConfig;
        private ILogger<SmsClient> _logger;

        public SmsClient(IOptions<SmsConfigOptions> smsConfig, ILogger<SmsClient> logger)
        {
            _smsConfig = smsConfig;
            _logger = logger;
            if (_smsConfig.Value.AccountSid == "")
            {
                _logger.LogInformation("SMS settings are not configured correctly");
                return;
            }
            TwilioClient.Init(_smsConfig.Value.AccountSid, _smsConfig.Value.AuthToken);
        }


        public void Send(string msg)
        {
            if (_smsConfig.Value.AccountSid != "")
            {
                MessageResource.Create(
                body: msg,
                from: new Twilio.Types.PhoneNumber(_smsConfig.Value.FromNumber),
                to: new Twilio.Types.PhoneNumber(_smsConfig.Value.ToNumber)
                );
            }
            else
            {
                _logger.LogInformation("SMS settings are not configured correctly");
            }
        }
    }
}
