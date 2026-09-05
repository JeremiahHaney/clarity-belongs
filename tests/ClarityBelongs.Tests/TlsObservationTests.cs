using Belongs.Shared.Observation;
using ClarityBelongs.Web.Domain;
using ClarityBelongs.Web.Observation;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;

namespace ClarityBelongs.Tests;

public sealed class TlsObservationTests
{
    private static readonly Target Target = new()
    {
        PrimaryUri = "https://93.184.216.34"
    };

    private static readonly SourceDefinition Source = new()
    {
        AdapterType = AdapterTypes.Tls,
        ConfigurationJson = "{}"
    };

    [Fact]
    public async Task Certificate_retrieval_captures_identity_and_expiration()
    {
        var expires = DateTime.UtcNow.AddDays(90);
        var engine = CreateEngine(() => CreateCertificate("example.test", expires));

        var result = await engine.ObserveAsync(
            new Uri(Target.PrimaryUri));

        Assert.Equal("93.184.216.34", result.Host);
        Assert.Contains("CN=example.test", result.Subject);
        Assert.False(string.IsNullOrWhiteSpace(result.Thumbprint));
        Assert.InRange(
            result.ExpiresUtc,
            expires.AddMinutes(-1),
            expires.AddMinutes(1));
    }

    [Fact]
    public async Task Certificate_change_changes_fingerprint()
    {
        var probe = new QueueTlsProbe(
            () => CreateCertificate("one.test", DateTime.UtcNow.AddDays(90)),
            () => CreateCertificate("two.test", DateTime.UtcNow.AddDays(120)));
        var engine = new TlsObservationEngine(
            new Belongs.Shared.Observation.PublicEndpointGuard(),
            probe);
        var adapter = new TlsObservationAdapter(engine);

        var first = await adapter.ObserveAsync(Target, Source);
        var second = await adapter.ObserveAsync(Target, Source);

        Assert.True(first.Success);
        Assert.True(second.Success);
        Assert.NotEqual(first.Fingerprint, second.Fingerprint);
    }

    [Theory]
    [InlineData(90, "Healthy")]
    [InlineData(10, "Warning")]
    public async Task Expiry_threshold_logic_is_applied(
        int days,
        string expectedStatus)
    {
        var engine = CreateEngine(() =>
            CreateCertificate(
                "threshold.test",
                DateTime.UtcNow.AddDays(days)));
        var adapter = new TlsObservationAdapter(engine);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.True(result.Success);
        Assert.Equal(expectedStatus, result.Status);
    }

    [Fact]
    public async Task Expired_certificate_is_down()
    {
        var engine = CreateEngine(() =>
            CreateCertificate(
                "expired.test",
                DateTime.UtcNow.AddDays(-1),
                DateTime.UtcNow.AddDays(-30)));
        var adapter = new TlsObservationAdapter(engine);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.False(result.Success);
        Assert.Equal("Down", result.Status);
        Assert.Equal("tls_expired", result.ErrorCode);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Invalid_or_failed_tls_is_reported_cleanly(bool authenticationFailure)
    {
        var probe = new ThrowingTlsProbe(authenticationFailure);
        var engine = new TlsObservationEngine(
            new Belongs.Shared.Observation.PublicEndpointGuard(),
            probe);
        var adapter = new TlsObservationAdapter(engine);

        var result = await adapter.ObserveAsync(Target, Source);

        Assert.False(result.Success);
        Assert.Equal("tls_error", result.ErrorCode);
    }

    [Fact]
    public async Task Unsupported_tls_target_is_rejected()
    {
        var engine = CreateEngine(() =>
            CreateCertificate(
                "unused.test",
                DateTime.UtcNow.AddDays(90)));
        var adapter = new TlsObservationAdapter(engine);
        var target = new Target
        {
            PrimaryUri = "ftp://93.184.216.34/file"
        };

        var result = await adapter.ObserveAsync(target, Source);

        Assert.False(result.Success);
        Assert.Equal("invalid_uri", result.ErrorCode);
    }

    private static TlsObservationEngine CreateEngine(
        Func<X509Certificate2> certificateFactory) =>
        new(
            new Belongs.Shared.Observation.PublicEndpointGuard(),
            new QueueTlsProbe(certificateFactory));

    private static X509Certificate2 CreateCertificate(
        string commonName,
        DateTime notAfter,
        DateTime? notBefore = null)
    {
        using var rsa = RSA.Create(2048);
        var request = new CertificateRequest(
            $"CN={commonName}",
            rsa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);
        var start = notBefore ?? DateTime.UtcNow.AddDays(-1);

        return request.CreateSelfSigned(
            new DateTimeOffset(start),
            new DateTimeOffset(notAfter));
    }

    private sealed class QueueTlsProbe(
        params Func<X509Certificate2>[] factories) : ITlsCertificateProbe
    {
        private readonly Queue<Func<X509Certificate2>> _factories = new(factories);

        public Task<X509Certificate2> GetCertificateAsync(
            Uri uri,
            IReadOnlyList<IPAddress> validatedAddresses,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_factories.Dequeue()());
        }
    }

    private sealed class ThrowingTlsProbe(bool authenticationFailure) : ITlsCertificateProbe
    {
        public Task<X509Certificate2> GetCertificateAsync(
            Uri uri,
            IReadOnlyList<IPAddress> validatedAddresses,
            CancellationToken cancellationToken = default)
        {
            if (authenticationFailure)
                throw new AuthenticationException("Certificate validation failed.");

            throw new IOException("TLS endpoint closed the connection.");
        }
    }
}
