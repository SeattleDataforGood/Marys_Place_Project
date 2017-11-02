using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Marys_Place_Uploader
{
    class AgencyClient
    {
        protected MD5 hashBuilder = MD5.Create();
        protected CookieAwareWebClient webClient = new CookieAwareWebClient();
        protected const string hashRegex = "name=\"agency_auth_unique_hash\" value=\"(?<1>[^\"']+)\"";

        public void login(string serverName, string username, string password)
        {
            string url = $"http://{serverName}/";

            string hashFromPage = getHashFromPage(url);

            string finalHash = calculateAuthHash(url, password, hashFromPage);

            sendLoginData(url, username, finalHash, hashFromPage);
        }

        protected string calculateAuthHash(string url, string password, string hashFromPage)
        {
            string passwordMd5 = ByteArrayToHex(hashBuilder.ComputeHash(Encoding.ASCII.GetBytes(password)));

            return ByteArrayToHex(hashBuilder.ComputeHash(Encoding.ASCII.GetBytes(passwordMd5 + hashFromPage)));
        }

        protected string getHashFromPage(string url)
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

        protected void sendLoginData(string url, string username, string passwordHash, string uniqueHash)
        {
            NameValueCollection values = new NameValueCollection();
            values.Add("agency_auth_unique_hash", uniqueHash);
            values.Add("agency_auth_username", username);
            values.Add("agency_auth_password", passwordHash);
            values.Add("logbutton", "Login");

            webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
            byte[] test = webClient.UploadValues(url, "POST", values);
            System.Console.WriteLine(Encoding.ASCII.GetString(test));
            webClient.Headers.Clear();
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


