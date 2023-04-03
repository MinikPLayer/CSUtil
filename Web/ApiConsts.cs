using CSUtil.DB;
using CSUtil.Web;
using System.Net;
using static CSUtil.Web.Api;

namespace CSUtil.Web
{
    public static class ApiConsts
    {
        public static ApiResult<object> ExpiredToken = new (HttpStatusCode.NetworkAuthenticationRequired, "!TE!");
        public static ApiResult<object> InvalidatedToken = new (HttpStatusCode.Unauthorized, "!TI!");
        public static ApiResult<object> AccessDenied = new (HttpStatusCode.Unauthorized, "!AD!");

        public static bool IsExpired<T>(this ApiResult<T> ret) => ret.StatusCode == ExpiredToken.StatusCode && ret.Message == ExpiredToken.Message;
        public static bool IsInvalidated<T>(this ApiResult<T> ret) => ret.StatusCode == InvalidatedToken.StatusCode && ret.Message == InvalidatedToken.Message;
        public static bool IsAccessDenied<T>(this ApiResult<T> ret) => ret.StatusCode == AccessDenied.StatusCode && ret.Message == AccessDenied.Message;
    }
}
