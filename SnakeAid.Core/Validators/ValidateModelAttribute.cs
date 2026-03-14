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

                    // Determine expected type once so we can produce friendlier messages
                    var expectedType = GetExpectedType(key, context);

                    foreach (var error in modelStateEntry.Value.Errors)
                    {
                        // Prefer friendly messages for JSON deserialization issues
                        if (!string.IsNullOrEmpty(error.ErrorMessage))
                        {
                            var friendly = TryGetFriendlyErrorMessage(key, error.ErrorMessage, expectedType);
                            if (!string.IsNullOrEmpty(friendly))
                            {
                                errorMessages.Add(friendly);
                                continue;
                            }

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
                        // Prefer a generic message for the root request object
                        if (key.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            errorMessages.Add("The request body is required and must be valid JSON.");
                        }
                        else
                        {
                            errorMessages.Add($"The {propertyName} field is required.");
                        }
                    }

                    if (errorMessages.Any())
                    {
                        errors[key] = errorMessages.ToArray(); // Keep as array
                    }
                }

                CreateValidationErrorResponse(context, errors);
            }
        }

        private string? TryGetFriendlyErrorMessage(string fieldPath, string rawMessage, Type? expectedType)
        {
            // Normalize common model binding messages for root request body
            if (rawMessage.Contains("The request field is required", StringComparison.OrdinalIgnoreCase) ||
                rawMessage.Contains("The request body is required", StringComparison.OrdinalIgnoreCase))
            {
                return "The request body is required and must be valid JSON.";
            }

            // Handle System.Text.Json conversion errors (common for wrong data types)
            if (expectedType != null && rawMessage.Contains("The JSON value could not be converted", StringComparison.OrdinalIgnoreCase))
            {
                string propertyName = fieldPath.Split('.').Last();
                return GetTypeSpecificMessage(propertyName, expectedType);
            }

            return null;
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

                // Handle JSON path keys like "$.role" coming from System.Text.Json deserialization errors
                // In that case, we treat it as a property of the first complex parameter (usually the request model).
                bool isJsonPath = fieldPath.StartsWith("$.", StringComparison.Ordinal);

                // For simple parameters (not a member access)
                if (!fieldPath.Contains('.') || isJsonPath)
                {
                    // If this is a JSON path, assume the first action parameter is the root object
                    if (isJsonPath)
                    {
                        var rootParamJson = actionDescriptor.MethodInfo.GetParameters().FirstOrDefault();
                        if (rootParamJson == null) return null;

                        var propertyName = fieldPath.Substring(2);
                        var property = rootParamJson.ParameterType.GetProperty(propertyName, BindingFlags.IgnoreCase | BindingFlags.Public | BindingFlags.Instance);
                        return property?.PropertyType;
                    }

                    var parameter = actionDescriptor.MethodInfo.GetParameters()
                        .FirstOrDefault(p => p.Name?.Equals(fieldPath, StringComparison.OrdinalIgnoreCase) == true);
                    return parameter?.ParameterType;
                }

                // For complex object properties (e.g. "request.role")
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
