
using System.Text.Json;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;

namespace Bookstore.Api.Authentication;

public sealed class DockerKeycloakConfigurationRetriever
    : IConfigurationRetriever<OpenIdConnectConfiguration>
{
    private readonly OpenIdConnectConfigurationRetriever _inner;
    private readonly string? _internalBaseUrl;

    public DockerKeycloakConfigurationRetriever(
        OpenIdConnectConfigurationRetriever inner,
        string? internalBaseUrl)
    {
        _inner = inner;
        _internalBaseUrl = internalBaseUrl;
    }

    public async Task<OpenIdConnectConfiguration> GetConfigurationAsync(
        string address,
        IDocumentRetriever retriever,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_internalBaseUrl))
        {
            return await OpenIdConnectConfigurationRetriever
                .GetAsync(address, retriever, cancellationToken);
        }

        var json = await retriever.GetDocumentAsync(
            address, cancellationToken);

        using var document = JsonDocument.Parse(json);

        var jwksUri = document.RootElement
            .GetProperty("jwks_uri")
            .GetString()!;

        var internalJwksUri =
            $"{_internalBaseUrl.TrimEnd('/')}{new Uri(jwksUri).PathAndQuery}";

        var properties = document.RootElement
            .EnumerateObject()
            .ToDictionary(
                property => property.Name,
                property => (object)property.Value.Clone());

        properties["jwks_uri"] = internalJwksUri;

        var updatedJson = JsonSerializer.Serialize(properties);

        var temporaryRetriever = new InlineMetadataRetriever(
            updatedJson,
            retriever);

        return await OpenIdConnectConfigurationRetriever.GetAsync(
            address,
            temporaryRetriever,
            cancellationToken);
    }

    private sealed class InlineMetadataRetriever : IDocumentRetriever
    {
        private readonly string _metadata;
        private readonly IDocumentRetriever _inner;

        public InlineMetadataRetriever(
            string metadata,
            IDocumentRetriever inner)
        {
            _metadata = metadata;
            _inner = inner;
        }

        public Task<string> GetDocumentAsync(
            string address,
            CancellationToken cancellationToken)
        {
            if (address.Contains(
                "/.well-known/openid-configuration"))
            {
                return Task.FromResult(_metadata);
            }

            return _inner.GetDocumentAsync(
                address,
                cancellationToken);
        }
    }
}
