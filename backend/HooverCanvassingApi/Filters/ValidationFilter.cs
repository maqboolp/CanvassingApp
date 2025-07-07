using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using HooverCanvassingApi.Middleware;

namespace HooverCanvassingApi.Filters
{
    public class ValidationFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            if (!context.ModelState.IsValid)
            {
                var errors = new Dictionary<string, string[]>();
                
                foreach (var (key, value) in context.ModelState)
                {
                    var errorMessages = value.Errors.Select(e => e.ErrorMessage).ToArray();
                    if (errorMessages.Any())
                    {
                        errors[key] = errorMessages;
                    }
                }

                throw new ValidationException("One or more validation errors occurred.", errors);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No action needed after execution
        }
    }
}