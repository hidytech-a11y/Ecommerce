using Ecommerce.Application.Common.Errors;

namespace Ecommerce.Application.Common.Exceptions;

public class NotFoundException : Exception
{
    public string ErrorCode { get; }

    public NotFoundException(
        string message,
        string errorCode = ErrorCodes.NotFound)
        : base(message)
    {
        ErrorCode = errorCode;
    }
}