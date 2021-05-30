using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Innovent_BL;
using Innovent_BL.EmailClient;
using Innovent_BL.SmsClient;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Innovent_Sim_Manager_Service
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private readonly IOptions<EmailConfigOptions> _emailOptions;
        private readonly IOptions<InnoventSettingOptions> _configOptions;
        private readonly IEmailSender _emailClient;
        private readonly ISmsClient _smsClient;
        public static List<string> queryList = new List<string>();

        public Worker(ILogger<Worker> logger, IOptions<InnoventSettingOptions> options, IOptions<EmailConfigOptions> emailOptions, IEmailSender emailClient, ISmsClient smsClient)
        {
            _logger = logger;
            _emailOptions = emailOptions;
            _configOptions = options;
            _emailClient = emailClient;
            _smsClient = smsClient;
            queryList.Add("query {{  sims(msisdn:\"{0}\", first:20) {{edges {{  node{{contactNumber:msisdn description active network {{name}}airtimeBalance dataBalanceInMb smsBalance  }}}}}}}}");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                var lowData = "";
                while (!stoppingToken.IsCancellationRequested)
                {
                    var cl = new GraphQLHttpClient(_configOptions.Value.GraphUrl, new NewtonsoftJsonSerializer());
                    cl.HttpClient.DefaultRequestHeaders.Add("simcontrol-api-key", _configOptions.Value.ApiKey);

                    foreach (string contactNumber in _configOptions.Value.ContactNumbers)
                    {

                        var content = new GraphQLRequest(string.Format(queryList[0], contactNumber));
                        var res = await cl.SendQueryAsync<Data>(content);
                        if (res.Data.Sims.Edges.Any())
                        {
                            var data = res.Data.Sims.Edges.First().Node;
                            _logger.LogInformation("\r\n\r\n\r\n");
                            _logger.LogInformation("\t\tDescription : " + data.Description);
                            _logger.LogInformation("\t\tNumber : " + data.ContactNumber);
                            _logger.LogInformation("\t\tActive: " + data.Active);
                            _logger.LogInformation("\t\tAirtime Balance : " + data.AirtimeBalance);
                            _logger.LogInformation("\t\tData Balance: " + data.DataBalanceInMb);
                            if (data.DataBalanceInMb == null || long.Parse(_configOptions.Value.MinimumLimit) > data.DataBalanceInMb)
                            {
                                lowData += string.Format("{0} - {1} - {2}Mb\r\n", data.ContactNumber, data.Description, data.DataBalanceInMb);
                            }
                        }
                    }
                    cl.Dispose();
                    if (lowData.Length > 1)
                    {
                        var mm = new Message(_emailOptions.Value.AdministratorEmails, "Sim Control - Low Data Report", "The following users has low data : \r\n" + lowData);
                        _emailClient.SendEmail(mm);
                        _logger.LogInformation("\t\tSending SMS...");
                        _smsClient.Send("You have a few low data items");
                    }
                    _logger.LogInformation("Next check will be at " + DateTime.Now.AddMilliseconds(_configOptions.Value.Delay));
                    await Task.Delay(_configOptions.Value.Delay, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message, ex);
            }
        }
    }
}
