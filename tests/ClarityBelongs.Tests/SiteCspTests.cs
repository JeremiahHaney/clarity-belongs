using ClarityBelongs.Web.Security;
using Xunit;

public sealed class SiteCspTests
{
    [Fact]
    public void NoncesAreIndependentAndBase64()
    {
        var first = SiteCsp.NewNonce();
        var second = SiteCsp.NewNonce();

        Assert.NotEqual(first, second);
        Assert.Equal(16, Convert.FromBase64String(first).Length);
    }

    [Fact]
    public void PolicyBlocksUnsafeExecution()
    {
        var policy = SiteCsp.Build(SiteCsp.NewNonce(), false);

        Assert.Contains("object-src 'none'", policy);
        Assert.Contains("frame-ancestors 'none'", policy);
        Assert.DoesNotContain("'unsafe-inline'", policy);
        Assert.DoesNotContain("'unsafe-eval'", policy);
        Assert.Contains("report-uri /csp/report", policy);
    }

    [Fact]
    public void LegacyReportStripsQueryAndPath()
    {
        var json = """{"csp-report":{"effective-directive":"script-src-elem","blocked-uri":"https://cdn.example.com/private/path?token=secret"}}""";
        var reports = SiteCsp.ParseReports(System.Text.Encoding.UTF8.GetBytes(json));

        Assert.Single(reports);
        Assert.Equal("script-src-elem", reports[0].Directive);
        Assert.Equal("cdn.example.com", reports[0].Target);
    }

    [Fact]
    public void ReportingApiPayloadIsBoundedAndSanitized()
    {
        var json = """[{"type":"csp-violation","body":{"effectiveDirective":"script-src","blockedURL":"inline"}}]""";
        var reports = SiteCsp.ParseReports(System.Text.Encoding.UTF8.GetBytes(json));

        Assert.Single(reports);
        Assert.Equal("inline", reports[0].Target);
        Assert.Empty(SiteCsp.ParseReports(System.Text.Encoding.UTF8.GetBytes("{broken")));
    }
}
