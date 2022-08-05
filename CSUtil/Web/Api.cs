using System;
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
            public T value { get; set; }

            public static implicit operator bool(Result<T> instance)
            {
                return instance.success;
            }

            public static implicit operator T(Result<T> instance)
            {
                return instance.value;
            }

            private Result(T value, HttpStatusCode code)
            {
                this.value = value;
                this.code = code;
                this.success = code == HttpStatusCode.OK;
            }

            public static Result<T> Success(T value) => new Result<T>(value, HttpStatusCode.OK);
            public static Result<T> Failure(HttpStatusCode code) => new Result<T>(default(T), code);
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

        public static async Task<Result<T>> Get<T>(string path, params Param[] ps)
        {
            try
            {
                var ret = await client.GetStreamAsync(GetURL(path, ps));
                var outValue = ConvertToValue<T>(await new StreamReader(ret).ReadToEndAsync());
                return Result<T>.Success(outValue);
            }
            catch(HttpRequestException e)
            {
                return Result<T>.Failure(e.StatusCode ?? HttpStatusCode.SeeOther);
            }
        }
    }
}