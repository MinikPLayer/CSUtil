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
        static string baseUrl = "http://127.0.0.1";
        static int port = 56789;
        public class Param
        {
            public string name { get; set; } = "";
            public string value { get; set; } = "";
        }

        public class Result<T>
        {
            public bool success { get; set; }
            public HttpStatusCode code { get; set; }
            public string failReason { get; set; }
            public T value { get; set; }

            public static implicit operator bool(Result<T> instance)
            {
                return instance.success;
            }

            public static implicit operator T(Result<T> instance)
            {
                return instance.value;
            }

            private Result(T value, HttpStatusCode code, string reason = "")
            {
                this.value = value;
                this.code = code;
                this.success = code == HttpStatusCode.OK;
                this.failReason = reason;
            }

            public static Result<T> Success(T value) => new Result<T>(value, HttpStatusCode.OK);
            public static Result<T> Failure(HttpStatusCode code, string reason = "") => new Result<T>(default(T), code, reason);
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
            
            if(!string.IsNullOrEmpty(token))
                query["token"] = token;
            
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

        static async Task<Result<T>> Send<T>(HttpRequestMessage msg)
        {
            HttpResponseMessage ret;
            try
            {
                ret = await client.SendAsync(msg);
            }
            catch (HttpRequestException e)
            {
                if(!e.StatusCode.HasValue)
                    return Result<T>.Failure(HttpStatusCode.RedirectMethod, "Can't connect to server");
                else
                    return Result<T>.Failure(e.StatusCode.Value);
            }

            var outString = await ret.Content.ReadAsStringAsync();
            if(!ret.IsSuccessStatusCode)
                return Result<T>.Failure(ret.StatusCode, outString);
                
            var outValue = ConvertToValue<T>(outString);
            return Result<T>.Success(outValue);
        }

        public static async Task<Result<T>> Post<T>(string path, params Param[] ps)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, GetURL(path, ps));
            return await Send<T>(request);
        }

        public static async Task<Result<T>> Get<T>(string path, params Param[] ps)
        {
            var request = new HttpRequestMessage(HttpMethod.Get, GetURL(path, ps));
            return await Send<T>(request);
        }

        static List<(HttpStatusCode code, string text)> errorCodesMeaningDict = new List<(HttpStatusCode, string text)>
        {
            (HttpStatusCode.Accepted, "OK"),
            (HttpStatusCode.RedirectMethod, "Connection refused")
        };
        public static string GetErrorCodesMeaning<T>(Result<T> ret, string defaultMessage = "?", params (HttpStatusCode code, string text)[] customDict)
        {
            if(!string.IsNullOrEmpty(ret.failReason))
                return ret.failReason;
        
            foreach (var option in errorCodesMeaningDict)
                if(ret.code == option.code)
                    return option.text;
            
            foreach(var item in customDict)
                if(ret.code == item.code)
                    return item.text;
                
            if(defaultMessage == "?")
                return ret.code.ToString();
                
            return defaultMessage;
        }
    }
}