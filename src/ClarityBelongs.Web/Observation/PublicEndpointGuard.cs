namespace ClarityBelongs.Web.Observation;

public sealed class PublicEndpointGuard
{
    private readonly Belongs.Shared.Observation.PublicEndpointGuard _shared = new();

    public Task ValidateAsync(
        Uri uri,
        CancellationToken cancellationToken = default) =>
        _shared.ValidateAsync(uri, cancellationToken);

    public Task ValidateHostAsync(
        string host,
        CancellationToken cancellationToken = default) =>
        _shared.ValidateHostAsync(host, cancellationToken);
}
