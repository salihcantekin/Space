using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Space.Abstraction.Extensions;

public static class TaskExtensions
{
    public static async ValueTask<object> AwaitAndReturnHandlerObject<TRequest>(this ValueTask<object> task, HandlerContext<TRequest> ctx)
    {
        try
        {
            return await task;
        }
        finally
        {
            HandlerContextPool<TRequest>.Return(ctx);
        }
    }

    public static async ValueTask<TResponse> AwaitAndReturnHanderInvoke<TRequest, TResponse>(this ValueTask<TResponse> task, HandlerContext<TRequest> ctx)
    {
        try
        {
            return await task;
        }
        finally
        {
            HandlerContextPool<TRequest>.Return(ctx);
        }
    }

    public static async ValueTask AwaitAndReturnNotificationInvoke<TRequest>(this ValueTask task, NotificationContext<TRequest> ctx)
    {
        try
        {
            await task;
        }
        finally
        {
            NotificationContextPool<TRequest>.Return(ctx);
        }
    }

    public static ValueTask<object> BoxValueTask<TResponse>(this ValueTask<TResponse> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object>(task.Result);
        }

        return new ValueTask<object>(task.AsTask()
                                         .ContinueWith(t => Unsafe.As<object>(t.Result),
                                                            TaskScheduler.Default));
    }
}
