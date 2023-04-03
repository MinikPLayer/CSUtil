using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Net;

namespace CSUtil.Web
{
    public class ApiResult<T>
    {
        public HttpStatusCode StatusCode;
        public string? Message = null;
        public T? Payload;

        public static explicit operator bool(ApiResult<T> ret) => ret.IsOk();

        public static implicit operator ActionResult(ApiResult<T> ret)
        {
            if ((bool)ret)
                return new ObjectResult(ret.Message) { StatusCode = (int)ret.StatusCode };

            return new ObjectResult(ret.Message) { StatusCode = (int)ret.StatusCode };
        }

        public bool IsOk() => this.StatusCode == HttpStatusCode.OK;

        /// <summary>
        /// For failures only
        /// </summary>
        /// <typeparam name="T2"></typeparam>
        /// <returns></returns>
        public ApiResult<T2> As<T2>() => new ApiResult<T2>(this.StatusCode, this.Message);

        public ApiResult(T payload)
        {
            StatusCode = HttpStatusCode.OK;
            Payload = payload;
        }

        public ApiResult(HttpStatusCode code, string message, T? payload = default)
        {
            StatusCode = code;
            Message = message;
            Payload = payload;
        }

        public override int GetHashCode() => HashCode.Combine(StatusCode, Message, Payload);
    }
}
