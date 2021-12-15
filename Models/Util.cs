using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace TickerTracker.Models
{
    public class Util
    {
        public static string getEnv(string key, string _default = null) => _Util.Instance().getEnv(key, _default);
        public static string genToken(int limit, bool md5 = true) => _Util.Instance().genToken(limit, md5);
        public static string Url(string path = "/", HttpRequest request = null) => _Util.Url(path, request);
        public static async Task<Option> getOption(string name) => await _Util.getOption(name);
        public static async Task<bool> deleteOption(string name) => await _Util.deleteOption(name);
        public static async Task<bool> setOption(string name, string value) => await _Util.setOption(name, value);
        public static async Task<string> getUrl(string url) => await _Util.getUrl(url);
        public static void Debug(object obj) => Console.WriteLine(JsonConvert.SerializeObject(obj));
        public static async Task<Dictionary<string, string>> getSupportedStocks() => await _Util.getSupportedStocks();
        public static async Task<Dictionary<string, Dictionary<string, string>>> getSupportedCrypto() => await _Util.getSupportedCrypto();
    }

    public class _Util
    {
        private static readonly _Util _instance = new _Util();

        IDictionary envVars;

        private _Util()
        {
            envVars = Environment.GetEnvironmentVariables();
        }

        public static _Util Instance()
        {
            return _instance;
        }

        public string getEnv( string key, string _default )
        {
            string value = envVars.Contains(key) ? envVars[key].ToString() : null;
            return String.IsNullOrEmpty(value) ? _default : value;
        }

        // max 32 chars if md5 is true
        public string genToken(int limit, bool md5=true)
        {
            using (RandomNumberGenerator rng = new RNGCryptoServiceProvider())
            {
                byte[] data = new byte[32];
                rng.GetBytes(data);
                var b64hash = Convert.ToBase64String(data);
                return (md5 ? _Util.md5(b64hash) : b64hash).Substring(0, limit);
            }
        }

        public static string md5(string str)
        {
            using (var md5 = MD5.Create())
            {
                byte[] bytes = md5.ComputeHash(System.Text.Encoding.ASCII.GetBytes(str));

                StringBuilder builder = new StringBuilder();

                for (int i = 0; i < bytes.Length; i++)
                {
                    builder.Append(bytes[i].ToString("X2"));
                }

                return builder.ToString();
            }
        }

        public static string Url(string path = "/", HttpRequest request = null)
        {
            return String.Format("{0}://{1}{2}",
                _Util.Instance().getEnv("HTTP_SCHEME", null != request ? request.Scheme : ""),
                _Util.Instance().getEnv("HTTP_HOST", null != request ? request.Host.Value : ""),
                path);
        }

        public static async Task<Option> getOption(string name)
        {
            var option = new Option { Name = name };
            return await option.Load() ? option : null;
        }

        public static async Task<bool> deleteOption(string name)
        {
            var option = new Option { Name = name };
            return await option.Delete();
        }

        public static async Task<bool> setOption(string name, string value)
        {
            var option = new Option { Name = name, Value = value };
            return await option.Save();
        }

        public static async Task<string> getUrl(string url)
        {
            try
            {
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }
            catch (Exception e)
            {
                Console.WriteLine("Util.JsonRequest error: {0}, {1}", e.ToString(), e.Message);
                return null;
            }
        }

        public static async Task<Dictionary<string, string>> getSupportedStocks()
        {
            var stocksRaw = await Models.Util.getOption("supported-stocks");
            var stocks = new Dictionary<string, string>();

            if (null != stocksRaw)
            {
                stocks = JsonConvert.DeserializeObject<Dictionary<string, string>>(stocksRaw.Value);
            }

            return stocks;
        }

        public static async Task<Dictionary<string, Dictionary<string, string>>> getSupportedCrypto()
        {
            var cryptoRaw = await Models.Util.getOption("supported-crypto");
            var crypto = new Dictionary<string, Dictionary<string, string>>();

            if (null != cryptoRaw)
            {
                crypto = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(cryptoRaw.Value);
            }

            return crypto;
        }
    }
}
