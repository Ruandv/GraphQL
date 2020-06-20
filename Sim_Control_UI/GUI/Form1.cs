using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Innovent_BL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            InnoventNotification.Visible = true;
            InnoventNotification.Icon = new System.Drawing.Icon("sync.ico");
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
            timer1.Interval = int.Parse(comboBox1.SelectedItem.GetValue().ToString()) * 60000;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            this.Hide();
            timer1.Enabled = true;
            notifyUser(null, null);
        }
        private async void notifyUser(object sender, EventArgs e)
        {
            var result = await getResult(_configOptions.Value.ContactNumber);
            InnoventNotification.BalloonTipText = String.Format("Contact Number :{0}\r\nData:{1}mb", result.ContactNumber, result.DataBalance);
            InnoventNotification.ShowBalloonTip(2000);
        }

        private async Task<SimResult> getResult(string contactNumber)
        {
            SimResult msg = new SimResult();
            var queryList = new List<string>();
            queryList.Add("query {{  sims(msisdn:\"{0}\", first:20) {{edges {{node{{contactNumber:msisdn description active network {{name}}airtimeBalance dataBalanceInMb smsBalance  }}}}}}}}");

            var cl = new GraphQLHttpClient(_configOptions.Value.GraphUrl, new NewtonsoftJsonSerializer());
            cl.HttpClient.DefaultRequestHeaders.Add("simcontrol-api-key", _configOptions.Value.ApiKey);

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
                msg.Active = data.Active;
                msg.AirtimeBalance = data.AirtimeBalance;
                msg.DataBalance = data.DataBalanceInMb;
                msg.ContactNumber = data.ContactNumber;
                msg.Description = data.Description;
            }
            cl.Dispose();
            return msg;
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            notifyUser(sender, e);
        }

        private void notifyIcon1_MouseDoubleClick_1(object sender, MouseEventArgs e)
        {
            this.Show();
        }

        private void InnoventNotification_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                showContextmenu(Screen.PrimaryScreen.WorkingArea.Width - 110, Screen.PrimaryScreen.WorkingArea.Height - 25);
            }
        }

        private void showContextmenu(int x, int y)
        {
            this.InnoventNotification.ContextMenuStrip = new ContextMenuStrip();
            this.InnoventNotification.ContextMenuStrip.Items.Add("Query Usage", null, notifyUser);
            this.InnoventNotification.ContextMenuStrip.Items.Add("View Report", null, showReport);
            this.InnoventNotification.ContextMenuStrip.Items.Add("Quit", null, ExitProgram);
            this.InnoventNotification.ContextMenuStrip.Show(x, y);
        }

        private void ExitProgram(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private async void showReport(object sender, EventArgs e)
        {
            this.Show();
            lblResults.Text = "Loading...";
            var results = new List<SimResult>();
            StringBuilder sb2 = new StringBuilder();
            var template = "<div class='card' style='width: 18rem;'><div class='card-body'><h5 class='card-title'>{0}</h5><div class='card-text'><p><strong>Contact Number :</strong> {1}</p><p><strong>Data Balance :</strong> {2}mb</p></div></div></div>";
            foreach (string s in _configOptions.Value.ContactNumbers)
            {
                var result = await getResult(s);
                results.Add(result);
                sb2.Append(String.Format(template, result.Description, result.ContactNumber, result.AirtimeBalance));
                lblResults.Text = string.Format("Loading {0} of {1}", Array.IndexOf(_configOptions.Value.ContactNumbers, s), _configOptions.Value.ContactNumbers.Length);
            }
            if (File.Exists(AppDomain.CurrentDomain.BaseDirectory + "\\DataReport.html"))
                File.Delete(AppDomain.CurrentDomain.BaseDirectory + "\\DataReport.html");

            var f = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "\\ReportTemplate.html");

            var sb = new StringBuilder();
            sb.Append(f.ReadToEnd());
            sb.Replace("{{cardHolder}}", sb2.ToString());
            sb.Replace("{{DateTime}}", DateTime.Now.ToString());
            StreamWriter sw = new StreamWriter(AppDomain.CurrentDomain.BaseDirectory + "\\DataReport.html");
            sw.Write(sb.ToString());
            sw.Flush();
            sw.Close();
            sw.Dispose();
            OpenWithDefaultProgram(AppDomain.CurrentDomain.BaseDirectory+ "\\DataReport.html");
        }

        public static void OpenWithDefaultProgram(string path)
        {
            Process fileopener = new Process();
            fileopener.StartInfo.FileName = "explorer";
            fileopener.StartInfo.Arguments = "\"" + path + "\"";
            fileopener.Start();
        }
    }
}
