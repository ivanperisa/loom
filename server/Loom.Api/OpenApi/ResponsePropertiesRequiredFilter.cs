using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Loom.Api.OpenApi;

/// <summary>
/// The API always writes every property of what it returns (nulls included), so in the contract they are all
/// required: the client gets <c>name: string | null</c>, not <c>name?: string | null</c>.
/// Requests are left alone; their optional parameters stay optional.
/// </summary>
public sealed class ResponsePropertiesRequiredFilter : ISchemaFilter
{
    public void Apply(IOpenApiSchema schema, SchemaFilterContext context)
    {
        if (schema is not OpenApiSchema { Properties.Count: > 0 } concrete) return;
        var name = context.Type.Name;
        if (name.EndsWith("Request", StringComparison.Ordinal) || name.EndsWith("Query", StringComparison.Ordinal)) return;

        concrete.Required ??= new HashSet<string>();
        foreach (var property in concrete.Properties!.Keys) concrete.Required.Add(property);
    }
}
