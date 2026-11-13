// Makes the OpenAPI document match the JSON we really send: nullable properties are left out when null, so they are optional, not required.
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace Tollgate.Api.Configuration;

// Why: JsonIgnoreCondition.WhenWritingNull (see AddTollgateWeb) drops null properties from responses, but the
// generator still lists them as "required". A generated client would then trust a field that is simply absent.
// The C# declaration (`Finding?`, `int?`) is the source of truth, so we read it instead of inspecting the schema shape.
public sealed class OmittedNullsSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(OpenApiSchema schema, OpenApiSchemaTransformerContext context, CancellationToken cancellationToken)
    {
        if (schema.Required is null)
        {
            return Task.CompletedTask;
        }

        foreach (var property in context.JsonTypeInfo.Properties.Where(property => property.IsGetNullable))
        {
            schema.Required.Remove(property.Name);
        }

        return Task.CompletedTask;
    }
}
