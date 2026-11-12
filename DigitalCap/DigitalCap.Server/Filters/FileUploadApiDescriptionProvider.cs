using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Linq;
using System.Reflection;

namespace DigitalCap.WebApi.Filters
{
    public class FileUploadApiDescriptionProvider : IApiDescriptionProvider
    {
        // Run after the default providers but before Swagger generation
        public int Order => -100;

        public void OnProvidersExecuting(ApiDescriptionProviderContext context)
        {
            // This runs before Swagger generation
        }

        public void OnProvidersExecuted(ApiDescriptionProviderContext context)
        {
            foreach (var apiDescription in context.Results)
            {
                // Remove IFormFile parameters from the API description
                // This prevents Swagger from trying to generate parameters for them
                // The operation filter will add them back as part of the request body
                var parametersToRemove = apiDescription.ParameterDescriptions
                    .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile) ||
                               (p.Source == BindingSource.Form && 
                                p.ModelMetadata?.ModelType == typeof(IFormFile)) ||
                               (p.Source == BindingSource.Form && 
                                apiDescription.ActionDescriptor is ControllerActionDescriptor cad &&
                                cad.MethodInfo.GetParameters().Any(pi => 
                                    pi.ParameterType == typeof(IFormFile) && 
                                    pi.GetCustomAttributes(typeof(FromFormAttribute), false).Any())))
                    .ToList();

                foreach (var param in parametersToRemove)
                {
                    apiDescription.ParameterDescriptions.Remove(param);
                }
            }
        }
    }
}
