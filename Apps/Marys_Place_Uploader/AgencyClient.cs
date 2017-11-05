using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Marys_Place_Uploader
{
    class AgencyClient
    {
        protected const string hashRegex = "name=\"agency_auth_unique_hash\" value=\"(?<1>[^\"']+)\"";
        protected const string reportsRegex = "name=\"agency_auth_unique_hash\" value=\"(?<1>[^\"']+)\"";

        protected readonly MD5 hashBuilder = MD5.Create();
        protected readonly CookieAwareWebClient webClient = new CookieAwareWebClient();
        protected string serverName;

        public void Login(string serverName, string username, string password)
        {
            this.serverName = serverName;
            string url = $"http://{serverName}/";

            string hashFromPage = GetHashFromPage(url);

            string finalHash = CalculateAuthHash(url, password, hashFromPage);

            SendLoginData(url, username, finalHash, hashFromPage);
        }

        public List<AgencyReport> GetReportList()
        {
            string html = webClient.DownloadString($"http://{serverName}/display.php?" +
                $"control[action]=list&control[object]=report&control[format]=data&control[id]=list&control[page]=display.php&control[object_references]=&control[step]=");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(html);
            var tableRows = htmlDoc.DocumentNode.SelectNodes("//table[@class='listMain listObjectReport']/tr[@class='generalData1' or @class='generalData2']/td[3]//a");

            List<AgencyReport> result = new List<AgencyReport>();
            foreach (var row in tableRows) {
                result.Add(new AgencyReport() { Title = row.Attributes["href"].Value, Url = row.Attributes["href"].Value });
            }
            return result;
        }

        internal void GetReportData(AgencyReport report)
        {
            string selectionHtml = webClient.DownloadString($"http://{serverName}/{report.Url}");
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(selectionHtml);

            string format = "1";
            string order = null;

            var formatOpts = htmlDoc.DocumentNode.SelectNodes("//select[@name='crv_template']//option");
            foreach (var option in formatOpts)
            {
                if (option.NextSibling.InnerText == "Tab-delimited file")
                {
                    format = option.Attributes["value"].Value;
                }
            }
            var orderOpt = htmlDoc.DocumentNode.SelectSingleNode("//select[@name='crv_order']/option");
            if (orderOpt != null)
            {
                order = orderOpt.Attributes["value"].Value;
            }

            NameValueCollection values = new NameValueCollection();
            values.Add("action", "generate");
            values.Add("crv_template", format);
            if (order != null)
            {
                values.Add("crv_order", order);
            }

            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            byte[] rawBytes = webClient.UploadValues($"http://{serverName}/{report.Url}", "POST", values);
            System.Console.WriteLine(Encoding.ASCII.GetString(rawBytes));
            webClient.Headers.Clear();
        }

        protected string CalculateAuthHash(string url, string password, string hashFromPage)
        {
            string passwordMd5 = ByteArrayToHex(hashBuilder.ComputeHash(Encoding.ASCII.GetBytes(password)));

            return ByteArrayToHex(hashBuilder.ComputeHash(Encoding.ASCII.GetBytes(passwordMd5 + hashFromPage)));
        }

        protected string GetHashFromPage(string url)
        {
            string loginPage = webClient.DownloadString(url);

            string hashFromPage;
            Match m = Regex.Match(loginPage, hashRegex);
            if (m.Success)
            {
                hashFromPage = m.Groups[1].Value;
            }
            else
            {
                throw new Exception("Login page not what I expected");
            }
            return hashFromPage;
        }

        protected string ByteArrayToHex(byte[] ba)
        {
            StringBuilder hex = new StringBuilder(ba.Length * 2);
            foreach (byte b in ba)
                hex.AppendFormat("{0:x2}", b);
            return hex.ToString();
        }

        protected void SendLoginData(string url, string username, string passwordHash, string uniqueHash)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("agency_auth_unique_hash", uniqueHash);
            values.Add("agency_auth_username", username);
            values.Add("agency_auth_password", passwordHash);
            values.Add("logbutton", "Login");

            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            byte[] rawBytes = webClient.UploadValues(url, "POST", values);
            webClient.Headers.Clear();
        }

    }

    class AgencyReport
    {
        public String Title
        {
            get; set;
        }
        public String Url
        {
            get; set;
        }
        public override string ToString()
        {
            return Title;
        }

    }

    public class CookieAwareWebClient : WebClient
    {
        public CookieContainer CookieContainer { get; set; } = new CookieContainer();

        protected override WebRequest GetWebRequest(Uri uri)
        {
            WebRequest request = base.GetWebRequest(uri);
            if (request is HttpWebRequest)
            {
                (request as HttpWebRequest).CookieContainer = CookieContainer;
            }
            return request;
        }
    }
}


