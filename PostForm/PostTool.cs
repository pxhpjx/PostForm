using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.IO;
using System.Configuration;
using System.Diagnostics;

namespace PostForm
{
    public partial class PostTool : Form
    {
        public PostTool()
        {
            InitializeComponent();

            try
            {
                string IsLocal = ConfigurationManager.AppSettings["IsLocal"].ToString();
                string DefaultPara = ConfigurationManager.AppSettings["DefaultPara"].ToString();
                string AccountNo = ConfigurationManager.AppSettings["AccountNo"].ToString();
                string Password = ConfigurationManager.AppSettings["Password"].ToString();
                string AutoGetCToken = ConfigurationManager.AppSettings["AutoGetCToken"].ToString();
                string AutoLogin = ConfigurationManager.AppSettings["AutoLogin"].ToString();

                if (!string.IsNullOrWhiteSpace(DefaultPara))
                    txtReqCon.Text = DefaultPara;
                if (!string.IsNullOrWhiteSpace(AccountNo))
                    txtAccountNo.Text = AccountNo;
                if (!string.IsNullOrWhiteSpace(Password))
                    txtPassword.Text = Password;

                List<ServerConfig> configs = LogRecord.ReadSerXmlLog<List<ServerConfig>>(Environment.CurrentDirectory + "\\Server.xml");
                if (configs != null && configs.Count > 0)
                {
                    List<string> list = new List<string>();
                    foreach (var v in configs.OrderBy(item => item.Description))
                        list.Add(v.Description + "|" + v.Url.Replace("http://", ""));
                    cbServerList.DataSource = list;
                }

                if (!string.IsNullOrWhiteSpace(AutoGetCToken))
                {
                    btnCToken_Click(this, new EventArgs());
                    if (!string.IsNullOrWhiteSpace(AutoLogin))
                        btnLogin_Click(this, new EventArgs());
                }
            }
            catch { }
        }

        private void btnPost_Click(object sender, EventArgs e)
        {
            txtInput.Text = txtInput.Text.Replace("\r\n", "");
            string PostUrl = GetPostUrl() + "/" + txtFunc.Text;

            string postData = txtReqCon.Text + "&ctoken=" + txtCToken.Text + "&utoken=" + txtUToken.Text + "&customerno=" + txtCustomerNo.Text + "&" + txtInput.Text;
            if (chkContextID.Checked)
                postData += "&contextid=" + txtContextID.Text;
            if (chkPwdPost.Checked)
                postData += "&Password=" + GetMd5Hash(txtPassword.Text);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(PostUrl));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            webRequest.UserAgent = "PP's PostTool";
            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream());
            txtOutput.Text = sr.ReadToEnd();
        }

        string GetPostUrl()
        {
            string PostUrl = "http://172.16.73.40:3333/home";
            if (rb11111.Checked)
                PostUrl = "http://172.16.73.40:11111/home";
            else
                if (rbList.Checked)
                    PostUrl = cbServerList.Text;
            if (PostUrl.Contains("|"))
                PostUrl = "http://" + PostUrl.Substring(PostUrl.IndexOf("|") + 1);
            return PostUrl;
        }

        private void btnCToken_Click(object sender, EventArgs e)
        {
            string PostUrl = GetPostUrl() + "/getctoken";

            string postData = txtReqCon.Text + "&ua=PP's PostTool";
            byte[] byteArray = Encoding.Default.GetBytes(postData);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(PostUrl));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            webRequest.UserAgent = "PP's PostTool";
            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string Message = sr.ReadToEnd();
            txtOutput.Text = Message;

            try
            {
                Message = Message.Substring(9);
                if (Message.IndexOf("\"") == 32)
                    txtCToken.Text = Message.Substring(0, 32);
            }
            catch { }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            string PostUrl = GetPostUrl() + (chkMobileC.Checked ? "/LoginCheckForMobile" : "/logincheck");


            string postData = txtReqCon.Text + "&ctoken=" + txtCToken.Text + "&account=" + txtAccountNo.Text + "&password=" + GetMd5Hash(txtPassword.Text) + "&" + txtInput.Text;
            byte[] byteArray = Encoding.Default.GetBytes(postData);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(PostUrl));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            webRequest.UserAgent = "PP's PostTool";
            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            string Message = sr.ReadToEnd();
            txtOutput.Text = Message;
            try
            {
                Message = Message.Substring(Message.IndexOf("UToken") + 9);
                if (Message.IndexOf("\"") == 32)
                {
                    txtUToken.Text = Message.Substring(0, 32);
                    Message = Message.Substring(Message.IndexOf("CustomerNo") + 13);
                    txtCustomerNo.Text = Message.Substring(0, 32);
                    Message = Message.Substring(Message.IndexOf("CustomerName") + 15);
                    txtCustomerName.Text = Message.Substring(0, Message.IndexOf("\""));
                }
            }
            catch { }
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            string ExecPath = "C:\\windows\\system32\\notepad.exe";
            string HelpFilePath = "C:\\ParamsHelper.txt";
            try
            {
                ExecPath = ConfigurationManager.AppSettings["ExecPath"].ToString();
                HelpFilePath = ConfigurationManager.AppSettings["HelpFilePath"].ToString();
            }
            catch { }
            Process pro = Process.Start(ExecPath, HelpFilePath);
            pro.Start();
        }

        public static string GetMd5Hash(string input)
        {
            using (System.Security.Cryptography.MD5 md5Hasher = System.Security.Cryptography.MD5.Create())
            {

                byte[] data = md5Hasher.ComputeHash(System.Text.Encoding.Default.GetBytes(input));

                string result = string.Format(@"{0}{1}{2}{3}{4}{5}{6}{7}{8}{9}{10}{11}{12}{13}{14}{15}",
                                data[0].ToString("x2"),
                                data[1].ToString("x2"),
                                data[2].ToString("x2"),
                                data[3].ToString("x2"),
                                data[4].ToString("x2"),
                                data[5].ToString("x2"),
                                data[6].ToString("x2"),
                                data[7].ToString("x2"),
                                data[8].ToString("x2"),
                                data[9].ToString("x2"),
                                data[10].ToString("x2"),
                                data[11].ToString("x2"),
                                data[12].ToString("x2"),
                                data[13].ToString("x2"),
                                data[14].ToString("x2"),
                                data[15].ToString("x2")
                                );
                return result;
            }
        }

        private void btnPostWap_Click(object sender, EventArgs e)
        {
            txtInput.Text = txtInput.Text.Replace("\r\n", "");
            string PostUrl = GetPostUrl() + WapPage() + ".aspx";
            string postData = txtReqCon.Text + "&uid=" + txtCustomerNo.Text + "&" + txtInput.Text;
            if (chkPwdPost.Checked)
                postData += "&Password=" + GetMd5Hash(txtPassword.Text);
            byte[] byteArray = Encoding.UTF8.GetBytes(postData);
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(new Uri(PostUrl));
            webRequest.Method = "POST";
            webRequest.ContentType = "application/x-www-form-urlencoded";
            webRequest.ContentLength = byteArray.Length;
            webRequest.UserAgent = "PP's PostTool";
            Stream newStream = webRequest.GetRequestStream();
            newStream.Write(byteArray, 0, byteArray.Length);
            newStream.Close();

            HttpWebResponse response = (HttpWebResponse)webRequest.GetResponse();
            StreamReader sr = new StreamReader(response.GetResponseStream(), Encoding.UTF8);
            txtOutput.Text = sr.ReadToEnd();
        }

        string WapPage()
        {
            if (!chkbi.Checked)
                return "do";
            return "bi";
        }

        private void txtPassword_TextChanged(object sender, EventArgs e)
        {
            string s = txtPassword.Text;
            if (s.Length >= 2 && s.Substring(s.Length - 2) == "vv")
            {
                txtPassword.PasswordChar = '*';
            }
            if (s.Length >= 2 && s.Substring(s.Length - 2) == "VV")
            {
                txtPassword.PasswordChar = (char)0;
            }
        }
    }


















    public class ServerConfig
    {
        public string Description { get; set; }
        public string Url { get; set; }
    }
}
