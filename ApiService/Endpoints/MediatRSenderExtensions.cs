using MediatR;

namespace ApiService.Endpoints;

public static class MediatRSenderExtensions
{
    public static Task<TResponse> SendRequest<TRequest, TResponse>(
        this ISender sender,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>, new()
    {
        return sender.Send(new TRequest(), cancellationToken);
    }

    public static Task<TResponse> SendRequest<TRequest, TResponse>(
        this ISender sender,
        Func<TRequest> requestFactory,
        CancellationToken cancellationToken)
        where TRequest : IRequest<TResponse>
    {
        return sender.Send(requestFactory(), cancellationToken);
    }
}