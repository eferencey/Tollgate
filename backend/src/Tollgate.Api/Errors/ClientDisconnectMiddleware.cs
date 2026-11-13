// A browser that navigates away or aborts a fetch cancels the request; the database call then throws
// OperationCanceledException. That is normal, not an error: nobody is left to answer, so it is swallowed here
// instead of becoming a logged 500 (and, under a debugger, a "user-unhandled exception" that freezes the server).
// Only cancellations caused by the client leaving are swallowed; any other exception passes through untouched.
namespace Tollgate.Api.Errors;

public sealed class ClientDisconnectMiddleware(RequestDelegate next)
{
    /// <summary>Non-standard code (nginx convention) for "the client closed the request"; only shows up in access logs.</summary>
    public const int ClientClosedRequest = 499;

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested)
        {
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = ClientClosedRequest;
            }
        }
    }
}
