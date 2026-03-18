using Microsoft.AspNetCore.Mvc;

public static class ProblemDetailsFactory
{
    public static ProblemDetails Create(
        HttpContext context,
        int status,
        string title,
        string detail,
        string errorCode)
    {
        return new ProblemDetails
        {
            Type = $"https://api.ecommerce.com/errors/{errorCode.ToLower()}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path
        };
    }
}