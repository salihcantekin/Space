using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Space.Abstraction.Extensions;

public static class TaskExtensions
{
    public static ValueTask<object> AwaitAndReturnHandlerObject<TRequest>(this ValueTask<object> task, HandlerContext<TRequest> ctx)
    {
        if (task.IsCompletedSuccessfully)
        {
            var res = task.Result;
            HandlerContextPool<TRequest>.Return(ctx);
            return new ValueTask<object>(res);
        }
        return Await(task, ctx);
        static async ValueTask<object> Await(ValueTask<object> t, HandlerContext<TRequest> c)
        { try { return await t; } finally { HandlerContextPool<TRequest>.Return(c); } }
    }

    public static ValueTask<TResponse> AwaitAndReturnHandlerInvoke<TRequest, TResponse>(this ValueTask<TResponse> task, HandlerContext<TRequest> ctx)
    {
        if (task.IsCompletedSuccessfully)
        {
            var res = task.Result;
            HandlerContextPool<TRequest>.Return(ctx);
            return new ValueTask<TResponse>(res);
        }
        return Await(task, ctx);
        static async ValueTask<TResponse> Await(ValueTask<TResponse> t, HandlerContext<TRequest> c)
        { try { return await t; } finally { HandlerContextPool<TRequest>.Return(c); } }
    }

    public static ValueTask AwaitAndReturnNotificationInvoke<TRequest>(this ValueTask task, NotificationContext<TRequest> ctx)
    {
        if (task.IsCompletedSuccessfully)
        {
            NotificationContextPool<TRequest>.Return(ctx);
            return new ValueTask();
        }
        return Await(task, ctx);
        static async ValueTask Await(ValueTask t, NotificationContext<TRequest> c)
        { try { await t; } finally { NotificationContextPool<TRequest>.Return(c); } }
    }

    public static ValueTask<TResponse> AwaitAndReturnPipelineInvoke<TRequest, TResponse>(this ValueTask<TResponse> task, PipelineContext<TRequest> ctx)
    {
        if (task.IsCompletedSuccessfully)
        {
            var res = task.Result;
            PipelineContextPool<TRequest>.Return(ctx);
            return new ValueTask<TResponse>(res);
        }
        return Await(task, ctx);
        static async ValueTask<TResponse> Await(ValueTask<TResponse> t, PipelineContext<TRequest> c)
        { try { return await t; } finally { PipelineContextPool<TRequest>.Return(c); } }
    }

    public static ValueTask<object> BoxValueTask<TResponse>(this ValueTask<TResponse> task)
    {
        if (task.IsCompletedSuccessfully)
        {
            return new ValueTask<object>(task.Result!);
        }
        return Await(task);
        static async ValueTask<object> Await(ValueTask<TResponse> t)
        { var res = await t; return res!; }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static async ValueTask<T> ContinueWithCast<T>(this ValueTask<object> task)
    {
        if (task.IsCompletedSuccessfully)
            return (T)task.Result!;
        var obj = await task;
        return (T)obj!;
    }
}
