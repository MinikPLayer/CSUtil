using Microsoft.AspNetCore.Mvc;
using NUnit.Framework;
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

    public class ApiResultTests
    {
        public static IEnumerable<object[]> GetTestsSources()
        {
            yield return new object[] { new ApiResult<string>("123"), true };
            yield return new object[] { new ApiResult<string>("abcdefg"), true };
            yield return new object[] { new ApiResult<string>(HttpStatusCode.Unauthorized, ""), false };
            yield return new object[] { new ApiResult<string>(HttpStatusCode.OK, ""), true };
        }

        [Test]
        [TestCaseSource(nameof(GetTestsSources))]
        public void IsOk(ApiResult<string> ret1, bool isOk) => Assert.That(ret1.IsOk(), Is.EqualTo(isOk));

        [Test]
        public void AsCasting()
        {
            var r1 = new ApiResult<string>(HttpStatusCode.AlreadyReported, "123");
            var r2 = r1.As<int>();

            Assert.That(r1.StatusCode, Is.EqualTo(r2.StatusCode));
            Assert.That(r1.Message, Is.EqualTo(r2.Message));

            Assert.That(r2.Payload, Is.EqualTo(default(int)));
        }
    }
}
