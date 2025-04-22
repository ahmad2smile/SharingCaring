using System.Text;
using JetBrains.Annotations;
using SharingCaring.Util;

namespace SharingCaring.Tests.Util;

[TestSubject(typeof(HttpUtils))]
public class HttpUtilsTest
{
    [Fact]
    public void BasicHeader_Works()
    {
        const string headerStr = "Content-Length: 98765\n";
        var header = Encoding.Default.GetBytes(headerStr.Split(":").First());

        var request = Encoding.Default.GetBytes("HTTP 1.1 GET\n" + headerStr);

        var result = HttpUtils.GetHeader(request, header);

        var resultStr = Encoding.Default.GetString(result);

        Assert.Equivalent("98765", resultStr);
    }
}