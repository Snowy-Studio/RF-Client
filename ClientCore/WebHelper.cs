using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ClientCore
{
    public class WebHelper
    {
        private static HttpClient _client = new HttpClient();

        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="strUrl">url</param>
        /// <param name="strContent">key1=value1&key2=value2</param>
        /// <returns></returns>
        public static async Task<string> HttpGet(string strUrl, string strContent)
        {
            try
            {
                if (strUrl.StartsWith("https"))
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string strURI = string.Format("{0}{1}", strUrl, string.IsNullOrEmpty(strContent) ? "" : "?" + strContent);
                var content = await _client.GetStringAsync(strURI);
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// GET请求
        /// </summary>
        /// <param name="strUrl"></param>
        /// <param name="dicData"></param>
        /// <returns></returns>
        public static async Task<string> HttpGet(string strUrl, Dictionary<string, string> dicData)
        {
            try
            {
                if (strUrl.StartsWith("https"))
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                StringBuilder sBuff = new StringBuilder();
                foreach (var kv in dicData)
                {
                    sBuff.AppendFormat("{0}={1}&", kv.Key, kv.Value);
                }

                string strURI = string.Format("{0}?{1}", strUrl, sBuff.ToString().TrimEnd('&'));
                var content = await _client.GetStringAsync(strURI);
                return content;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// POST请求
        /// </summary>
        /// <param name="strUrl">url</param>
        /// <param name="dicData">字典键值对</param>
        /// <returns></returns>
        public static async Task<string> HttpPost(string strUrl, Dictionary<string, string> dicData)
        {
            try
            {
                if (strUrl.StartsWith("https"))
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                string strURI = string.Format("{0}", strUrl);
                var data = new FormUrlEncodedContent(dicData);
                var response = await _client.PostAsync(strURI, data);
                if (response.IsSuccessStatusCode)
                {
                    return response.Content.ReadAsStringAsync().Result;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return string.Empty;
        }

        /// <summary>
        /// 下载文件
        /// </summary>
        /// <param name="strUrlFile"></param>
        /// <param name="strLocFile"></param>
        /// <returns></returns>
        public static bool HttpDownFile(string strUrlFile, string strLocFile)
        {
            try
            {
                if (strUrlFile.StartsWith("https"))
                    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                string strLocDir = Path.GetDirectoryName(strLocFile);
                if(!Directory.Exists(strLocDir))
                    Directory.CreateDirectory(strLocDir);

                var response = _client.GetAsync(new Uri(strUrlFile)).Result;
                if(response.IsSuccessStatusCode)
                {
                    using (var fs = File.Create(strLocFile))
                    {
                        var stream = response.Content.ReadAsStreamAsync().Result;
                        stream.CopyTo(fs);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            return false;
        }
    }
}
