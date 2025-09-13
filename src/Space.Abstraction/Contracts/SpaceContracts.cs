using System.Threading.Tasks;

namespace Space.Abstraction.Contracts;

public interface IRequest<out TResponse>;

public interface IPipelineHandler<TRequest, TResponse>
{
    ValueTask<TResponse> HandlePipeline(PipelineContext<TRequest> ctx, PipelineDelegate<TRequest, TResponse> next);
}

public interface IHandler<TRequest, TResponse>
    where TRequest : notnull, IRequest<TResponse>
    where TResponse : notnull
{
    ValueTask<TResponse> Handle(HandlerContext<TRequest> ctx);
}

public interface INotificationHandler<TRequest>
{
    ValueTask HandleNotification(NotificationContext<TRequest> ctx);
}