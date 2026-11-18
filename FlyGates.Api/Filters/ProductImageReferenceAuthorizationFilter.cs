using System.Security.Cryptography;
using System.Text;
using FlyGates.Infraestructure.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace FlyGates.Filters;

public sealed class ProductImageReferenceAuthorizationFilter(
    IOptionsMonitor<AwsS3Options> options) : IAuthorizationFilter
{
    public const string HeaderName = "X-Product-Images-Key";

    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var configuredKey = options.CurrentValue.ProductImageReferenceApiKey;
        if (string.IsNullOrWhiteSpace(configuredKey))
        {
            context.Result = Failure(StatusCodes.Status503ServiceUnavailable,
                "Shared product image authorization is not configured.");
            return;
        }

        var headers = context.HttpContext.Request.Headers;
        if (!headers.TryGetValue(HeaderName, out var suppliedKeys)
            || suppliedKeys.Count != 1
            || !Matches(configuredKey, suppliedKeys[0] ?? string.Empty))
        {
            context.Result = Failure(StatusCodes.Status401Unauthorized,
                "A valid shared product image key is required.");
        }
    }

    private static bool Matches(string expected, string supplied) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(expected)),
            SHA256.HashData(Encoding.UTF8.GetBytes(supplied)));

    private static ObjectResult Failure(int status, string title)
    {
        var result = new ObjectResult(new ProblemDetails { Status = status, Title = title })
        {
            StatusCode = status,
        };
        result.ContentTypes.Add("application/problem+json");
        return result;
    }
}
