using Ecommerce.Application.Common.Errors;

namespace Ecommerce.Application.Common.Exceptions;

public class UnauthorizedException : Exception
{
    public string ErrorCode { get; }

    public UnauthorizedException(
        string message,
        string errorCode = ErrorCodes.Unauthorized)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}