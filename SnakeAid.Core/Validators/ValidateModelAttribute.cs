using SnakeAid.Core.Meta;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SnakeAid.Core.Validators
{
    public class ValidateModelAttribute : ActionFilterAttribute
    {
        // This class is used to validate the model attributes
        // If the model is not valid, it will return a bad request response
        public override void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = new Dictionary<string, string[]>(); // Changed to string[]

                // Always run this validation, even if the model state is invalid
                foreach (var modelStateEntry in context.ModelState)
                {
                    var key = modelStateEntry.Key;
                    var errorMessages = new List<string>();

                    foreach (var error in modelStateEntry.Value.Errors)
                    {
                        if (!string.IsNullOrEmpty(error.ErrorMessage))
                        {
                            errorMessages.Add(error.ErrorMessage);
                        }
                        else if (error.Exception != null)
                        {
                            string customMessage = GetCustomTypeErrorMessage(key, error.Exception, context);
                            errorMessages.Add(customMessage);
                        }
                    }

                    // Add default message if no specific errors but field is invalid
                    if (!errorMessages.Any() && modelStateEntry.Value.ValidationState == ModelValidationState.Invalid)
                    {
                        string propertyName = key.Split('.').Last();
                        errorMessages.Add($"The {propertyName} field is required.");
                    }

                    if (errorMessages.Any())
                    {
                        errors[key] = errorMessages.ToArray(); // Keep as array
                    }
                }

                CreateValidationErrorResponse(context, errors);
            }
        }

        private string GetCustomTypeErrorMessage(string fieldPath, Exception exception, ActionExecutingContext context)
        {
            string propertyName = fieldPath.Split('.').Last();

            // Try to get expected type from action method parameters
            Type? expectedType = GetExpectedType(fieldPath, context);

            return exception switch
            {
                FormatException when expectedType != null => GetTypeSpecificMessage(propertyName, expectedType),
                InvalidCastException when expectedType != null => GetTypeSpecificMessage(propertyName, expectedType),
                ArgumentException when expectedType != null => GetTypeSpecificMessage(propertyName, expectedType),
                _ => $"The field {propertyName} has an invalid value."
            };
        }

        private Type? GetExpectedType(string fieldPath, ActionExecutingContext context)
        {
            try
            {
                var actionDescriptor = context.ActionDescriptor as Microsoft.AspNetCore.Mvc.Controllers.ControllerActionDescriptor;
                if (actionDescriptor?.MethodInfo == null) return null;

                // For simple parameters
                if (!fieldPath.Contains('.'))
                {
                    var parameter = actionDescriptor.MethodInfo.GetParameters()
                        .FirstOrDefault(p => p.Name?.Equals(fieldPath, StringComparison.OrdinalIgnoreCase) == true);
                    return parameter?.ParameterType;
                }

                // For complex object properties
                var parts = fieldPath.Split('.');
                var rootParam = actionDescriptor.MethodInfo.GetParameters()
                    .FirstOrDefault(p => p.Name?.Equals(parts[0], StringComparison.OrdinalIgnoreCase) == true);

                if (rootParam == null) return null;

                Type currentType = rootParam.ParameterType;
                for (int i = 1; i < parts.Length; i++)
                {
                    var property = currentType.GetProperty(parts[i], BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                    if (property == null) return null;
                    currentType = property.PropertyType;
                }

                return currentType;
            }
            catch
            {
                return null;
            }
        }

        private string GetTypeSpecificMessage(string propertyName, Type expectedType)
        {
            // Handle nullable types
            Type underlyingType = Nullable.GetUnderlyingType(expectedType) ?? expectedType;

            return underlyingType.Name switch
            {
                nameof(Int32) or nameof(Int64) or nameof(Int16) => $"The field {propertyName} must be a valid integer.",
                nameof(Double) or nameof(Single) or nameof(Decimal) => $"The field {propertyName} must be a valid number.",
                nameof(DateTime) => $"The field {propertyName} must be a valid date and time (yyyy-MM-dd HH:mm:ss).",
                nameof(Boolean) => $"The field {propertyName} must be true or false.",
                nameof(Guid) => $"The field {propertyName} must be a valid GUID format.",
                _ when underlyingType.IsEnum => $"The field {propertyName} must be one of: {string.Join(", ", Enum.GetNames(underlyingType))}.",
                _ => $"The field {propertyName} has an invalid value."
            };
        }

        private void CreateValidationErrorResponse(ActionExecutingContext context, Dictionary<string, string[]> errors)
        {
            var clientError = new ClientErrorResponse
            {
                ErrorCode = "VALIDATION_ERROR",
                Timestamp = DateTime.UtcNow,
                ValidationErrors = errors
            };

            var response = new ApiResponse<object>
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity,
                Message = "Validation failed",
                IsSuccess = false,
                Data = null,
                Error = clientError
            };

            context.Result = new ObjectResult(response)
            {
                StatusCode = StatusCodes.Status422UnprocessableEntity
            };
        }
    }
}
