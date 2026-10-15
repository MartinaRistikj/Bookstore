
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Bookstore.Api.OpenApi;

public class SecurityRequirementsOperationFilter : IOperationFilter
{
    public void Apply(
        OpenApiOperation operation,
        OperationFilterContext context)
    {
        var policies = context.MethodInfo
            .GetCustomAttributes(true)
            .OfType<AuthorizeAttribute>()
            .Select(a => a.Policy)
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (policies.Count == 0)
            return;

        var isSearch = policies.Contains("BookSearch");

        var schemeName = isSearch
            ? "SearchOAuth"
            : "CrudOAuth";

        var scopes = isSearch
            ? new List<string> { "bookstore.search" }
            : new List<string>();

        operation.Security =
        [
            new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference(
                    schemeName, context.Document)] = scopes
            }
        ];

        operation.Responses ??= [];

        operation.Responses.TryAdd("401",
            new OpenApiResponse
            {
                Description = "Unauthorized"
            });

        operation.Responses.TryAdd("403",
            new OpenApiResponse
            {
                Description = "Forbidden"
            });
    }
}
