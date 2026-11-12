using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace DigitalCap.WebApi.Filters
{
    public class FileUploadOperationFilter : IOperationFilter
    {
        public void Apply(OpenApiOperation operation, OperationFilterContext context)
        {
            var formFileParameters = context.MethodInfo.GetParameters()
                .Where(p => p.ParameterType == typeof(IFormFile) ||
                           p.GetCustomAttributes(typeof(FromFormAttribute), false).Any())
                .ToList();

            if (formFileParameters.Any())
            {
                // Remove existing parameters that are IFormFile or have [FromForm]
                var parametersToRemove = operation.Parameters
                    .Where(p => formFileParameters.Any(fp => fp.Name == p.Name))
                    .ToList();

                foreach (var param in parametersToRemove)
                {
                    operation.Parameters.Remove(param);
                }

                // Add multipart/form-data content type
                // Check if RequestBody is null or doesn't have multipart/form-data
                if (operation.RequestBody == null ||
                    !operation.RequestBody.Content.ContainsKey("multipart/form-data"))
                {
                    var content = new OpenApiMediaType
                    {
                        Schema = new OpenApiSchema
                        {
                            Type = JsonSchemaType.Object,
                            Properties = new Dictionary<string, IOpenApiSchema>()
                        }
                    };

                    // Add all form parameters to the schema
                    foreach (var param in formFileParameters)
                    {
                        var paramName = param.Name;
                        var paramType = param.ParameterType;

                        if (paramType == typeof(IFormFile))
                        {
                            content.Schema.Properties[paramName] = new OpenApiSchema
                            {
                                Type = JsonSchemaType.String,
                                Format = "binary",
                                Description = "File to upload"
                            };
                        }
                        else
                        {
                            // For other types, determine the schema
                            var schema = GetSchemaForType(paramType);
                            content.Schema.Properties[paramName] = schema;
                        }
                    }

                    // Mark required properties
                    var requiredParams = formFileParameters
                        .Where(p => !p.HasDefaultValue && !p.IsOptional)
                        .Select(p => p.Name)
                        .ToList();

                    // Initialize RequestBody if it's null, or update existing one
                    if (operation.RequestBody == null)
                    {
                        operation.RequestBody = new OpenApiRequestBody
                        {
                            Content = new Dictionary<string, OpenApiMediaType>(),
                            Required = true
                        };
                    }

                    // Add multipart/form-data content
                    operation.RequestBody.Content["multipart/form-data"] = content;
                }
            }
        }

        private OpenApiSchema GetSchemaForType(Type type)
        {
            if (type == typeof(string))
            {
                return new OpenApiSchema { Type = JsonSchemaType.String };
            }
            else if (type == typeof(int) || type == typeof(int?))
            {
                return new OpenApiSchema { Type = JsonSchemaType.Integer, Format = "int32" };
            }
            else if (type == typeof(bool) || type == typeof(bool?))
            {
                return new OpenApiSchema { Type = JsonSchemaType.Boolean };
            }
            else if (type.IsEnum)
            {
                return new OpenApiSchema { Type = JsonSchemaType.String };
            }
            else
            {
                return new OpenApiSchema { Type = JsonSchemaType.String };
            }
        }
    }
}
