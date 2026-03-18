using Ecommerce.Application.Common.Errors;

namespace Ecommerce.Application.Common.Exceptions;

public class BadRequestException : Exception
{
    public string ErrorCode { get; }

    public BadRequestException(
        string message,
        string errorCode = ErrorCodes.ValidationFailed)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}