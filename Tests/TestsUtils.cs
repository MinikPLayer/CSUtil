using CSUtil.Web;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSUtil.Tests
{
    public static class TestsUtils
    {
        public static void Assert<T>(this ApiResult<T> result, bool checkContent = true)
        {
            NUnit.Framework.Assert.IsTrue(result.IsOk(), result.StatusCode + " - " + result.Message);
            if (checkContent)
                NUnit.Framework.Assert.NotNull(result.Payload);
        }
    }
}
