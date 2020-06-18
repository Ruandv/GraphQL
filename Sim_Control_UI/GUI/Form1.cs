using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Innovent_BL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GUI
{
    public partial class Form1 : Form
    {
        private ILogger _logger;
        private IOptions<InnoventSettingOptions> _configOptions;

        public Form1(ILoggerFactory logger, IOptions<InnoventSettingOptions> options)
        {
            this.Icon = new System.Drawing.Icon("sync.ico");
            this._logger = logger.CreateLogger("F1");
            this._configOptions = options;
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Text = "Innovent Sim Notification";
            this.textBox1.Text = _configOptions.Value.ContactNumber.ToString();
            this.comboBox1.SelectedIndex = _configOptions.Value.Delay;
        }

        private void textBox1_Leave(object sender, EventArgs e)
        {
            AddOrUpdateAppSetting("Innovent_Settings:ContactNumber", textBox1.Text);
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            AddOrUpdateAppSetting("Innovent_Settings:Delay", comboBox1.SelectedIndex);
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            InnoventSim.Visible = true;
            timer1.Interval = 60000 * int.Parse(comboBox1.SelectedItem.GetValue().ToString());
            timer1.Enabled = true;
            await getResult();
        }

        private async Task getResult()
        {
            var queryList = new List<string>();
            queryList.Add("query {{  sims(msisdn:\"{0}\", first:20) {{edges {{node{{contactNumber:msisdn description active network {{name}}airtimeBalance dataBalanceInMb smsBalance  }}}}}}}}");

            var cl = new GraphQLHttpClient(_configOptions.Value.GraphUrl, new NewtonsoftJsonSerializer());
            cl.HttpClient.DefaultRequestHeaders.Add("simcontrol-api-key", _configOptions.Value.ApiKey);

            foreach (string contactNumber in new[] { textBox1.Text })
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

                    InnoventSim.BalloonTipText = string.Format("{0} \r\n {1} \r\n {2} Mb\r\n", data.ContactNumber, data.Description, data.DataBalanceInMb); ;
                    InnoventSim.ShowBalloonTip(2000);
                }
            }
            cl.Dispose();
        }
        public static void AddOrUpdateAppSetting<T>(string key, T value)
        {
            try
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, @"configuration\appsettings.json");
                string json = File.ReadAllText(filePath);
                dynamic jsonObj = Newtonsoft.Json.JsonConvert.DeserializeObject(json);

                var sectionPath = key.Split(":")[0];
                if (!string.IsNullOrEmpty(sectionPath))
                {
                    var keyPath = key.Split(":")[1];
                    jsonObj[sectionPath][keyPath] = value;
                }
                else
                {
                    jsonObj[sectionPath] = value; // if no sectionpath just set the value
                }
                string output = Newtonsoft.Json.JsonConvert.SerializeObject(jsonObj, Newtonsoft.Json.Formatting.Indented);
                File.WriteAllText(filePath, output);

            }
            catch (ConfigurationErrorsException)
            {
                Console.WriteLine("Error writing app settings");
            }
        }
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            getResult().ConfigureAwait(false);
        }
    }
}
