using System.Data.Common;
using Trackwise.Shared.Models;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Trackwise.Application.Behaviours;

public sealed class UnhandledExceptionBehaviour<TRequest, TResponse>(
    ILogger<UnhandledExceptionBehaviour<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : IResult, new()
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        try
        {
            return await next(cancellationToken);
        }
        catch (Exception ex)
        {
            var requestName = typeof(TRequest).Name;
            logger.LogError(ex, "Unhandled Exception for Request {Name} {@Request}", requestName, request);

            // A database error's text is SQL, not something a dispatcher or a crew can act on.
            if (ex is DbException || ex.InnerException is DbException)
            {
                return new TResponse { Error = DatabaseFaultMessage, ErrorCode = ErrorCodes.ServerFault };
            }

            return new TResponse { Error = ex.Message };
        }
    }

    private const string DatabaseFaultMessage =
        "The server couldn't save this. Try again, and tell the office if it keeps happening.";
}
