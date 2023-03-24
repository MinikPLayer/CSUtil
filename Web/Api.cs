using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;

namespace CSUtil.Web
{
    public static class Api
    {
        private static readonly HttpClient client = new HttpClient();
        public static string baseUrl = "http://localhost";
        public static int port = 56789;
        public class Param
        {
            public string name { get; set; } = "";
            public string value { get; set; } = "";
        }

        public static string token = "";

        public static Param ToApiParam<T>(this string s, T value)
        {
            return new Param()
            {
                name = s,
                value = value.ToString() ?? ""
            };
        }

        static Uri GetURL(string path, params Param[] ps)
        {
            string address = baseUrl.TrimEnd('/') + "/" + path;
            var builder = new UriBuilder(address);
            builder.Port = port;
            
            var query = HttpUtility.ParseQueryString(builder.Query);

            for(int i = 0;i<ps.Length;i++)
                query[ps[i].name] = ps[i].value;
                
            builder.Query = query.ToString() ?? "";
            return new Uri(builder.ToString());
        }

        static T ConvertToValue<T>(string value)
        {
            if(typeof(T) == typeof(string))
                return (T)Convert.ChangeType(value, typeof(T));
                
            return JsonConvert.DeserializeObject<T>(value);
        }

        static async Task<ApiResult<T>> Send<T>(HttpRequestMessage msg)
        {
            HttpResponseMessage ret;
            try
            {
                if(token != null)
                    msg.Headers.Add("Authorization", "Bearer " + token);

                ret = await client.SendAsync(msg);
            }
            catch (HttpRequestException e)
            {
                if(!e.StatusCode.HasValue)
                    return new ApiResult<T>(HttpStatusCode.RedirectMethod, "Can't connect to server");
                else
                    return new ApiResult<T>(e.StatusCode.Value, "");
            }

            var outString = await ret.Content.ReadAsStringAsync();
            if(!ret.IsSuccessStatusCode)
                return new ApiResult<T>(ret.StatusCode, outString);
                
            var outValue = ConvertToValue<T>(outString);
            return new ApiResult<T>(outValue);
        }

        public static async Task<ApiResult<T>> Post<T>(string path, params Param[] ps)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetURL(path, ps));
            return await Send<T>(request);
        }

        public static async Task<ApiResult<T>> Get<T>(string path, params Param[] ps)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetURL(path, ps));
            return await Send<T>(request);
        }

        static List<(HttpStatusCode code, string text)> errorCodesMeaningDict = new List<(HttpStatusCode, string text)>
        {
            (HttpStatusCode.Accepted, "OK"),
            (HttpStatusCode.RedirectMethod, "Connection refused")
        };
        public static string GetErrorCodesMeaning<T>(ApiResult<T> ret, string defaultMessage = "?", params (HttpStatusCode code, string text)[] customDict)
        {
            if(!string.IsNullOrEmpty(ret.Message))
                return ret.Message;
        
            foreach (var option in errorCodesMeaningDict)
                if(ret.StatusCode == option.code)
                    return option.text;
            
            foreach(var item in customDict)
                if(ret.StatusCode == item.code)
                    return item.text;
                
            if(defaultMessage == "?")
                return ret.StatusCode.ToString();
                
            return defaultMessage;
        }
    }
}