namespace Ecommerce.Application.Common.Responses;

public class ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public IEnumerable<string>? Errors { get; init; }

    public static ApiResponse<T> SuccessResponse(T data, string message = "")
        => new()
        {
            Success = true,
            Data = data,
            Message = message
        };

    public static ApiResponse<T> FailureResponse(
        IEnumerable<string> errors,
        string message = "")
        => new()
        {
            Success = false,
            Errors = errors,
            Message = message
        };
}