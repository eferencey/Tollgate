// A client that aborts its request must not turn into an error; every other failure must still surface.
using Microsoft.AspNetCore.Http;
using Tollgate.Api.Errors;

namespace Tollgate.Api.Tests;

public class ClientDisconnectTests
{
    [Fact]
    public async Task A_cancellation_caused_by_the_client_leaving_is_swallowed()
    {
        using var abort = new CancellationTokenSource();
        var context = new DefaultHttpContext { RequestAborted = abort.Token };
        var middleware = new ClientDisconnectMiddleware(_ =>
        {
            abort.Cancel();
            throw new OperationCanceledException(abort.Token);
        });

        await middleware.InvokeAsync(context);

        Assert.Equal(ClientDisconnectMiddleware.ClientClosedRequest, context.Response.StatusCode);
    }

    [Fact]
    public async Task A_cancellation_while_the_client_is_still_connected_is_a_real_failure()
    {
        var context = new DefaultHttpContext();
        var middleware = new ClientDisconnectMiddleware(_ => throw new OperationCanceledException("our own timeout"));

        await Assert.ThrowsAsync<OperationCanceledException>(() => middleware.InvokeAsync(context));
    }

    [Fact]
    public async Task Other_exceptions_pass_through_even_if_the_client_has_left()
    {
        using var abort = new CancellationTokenSource();
        abort.Cancel();
        var context = new DefaultHttpContext { RequestAborted = abort.Token };
        var middleware = new ClientDisconnectMiddleware(_ => throw new InvalidOperationException("db locked"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => middleware.InvokeAsync(context));
    }
}
