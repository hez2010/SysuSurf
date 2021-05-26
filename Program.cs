using System;
using System.Text;
using SysuH3c.Eap;
using static SysuH3c.Utils.AssertHelpers;

namespace SysuH3c
{
    class Program
    {
        static void Main(string[] args)
        {
            var body = EapBody.CreateRequestBody(Encoding.UTF8.GetBytes("test"));
            Assert(EapBody.GetBodyType(body.Span) == EapBodyType.Request);
            Assert(Encoding.UTF8.GetString(EapBody.GetBodyData(body.Span)) == "test");
        }
    }
}
