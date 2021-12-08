using System;
using System.Collections;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;

namespace TickerTracker.Models
{
    public class Util
    {
        public static string getEnv(string key, string _default = null) => _Util.Instance().getEnv(key, _default);
        public static string genToken(int limit, bool md5 = true) => _Util.Instance().genToken(limit, md5);
        public static string Url(string path = "/", HttpRequest request = null) => _Util.Url(path, request);
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
                _Util.Instance().getEnv("HTTP_HOST", null != request ? request.Scheme : ""),
                path);
        }
    }
}
