using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Filters;

public class ApiExceptionFilter : IExceptionFilter
{
    private readonly ITempDataDictionaryFactory _tempDataFactory;
    private readonly ILogger<ApiExceptionFilter> _logger;

    public ApiExceptionFilter(ITempDataDictionaryFactory tempDataFactory, ILogger<ApiExceptionFilter> logger)
    {
        _tempDataFactory = tempDataFactory;
        _logger = logger;
    }

    public void OnException(ExceptionContext context)
    {
        switch (context.Exception)
        {
            case ApiSessionExpiredException:
                context.HttpContext.Session.Clear();
                var tempData = _tempDataFactory.GetTempData(context.HttpContext);
                tempData["Error"] = "Your session has expired. Please log in again.";
                context.Result = new RedirectToActionResult("Login", "Account", null);
                context.ExceptionHandled = true;
                break;

            case ApiUnavailableException ex:
                _logger.LogError(ex, "RaceDay API unavailable");
                context.Result = new ViewResult { ViewName = "ApiUnavailable", StatusCode = 503 };
                context.ExceptionHandled = true;
                break;
        }
    }
}
