using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Registry.Dispatchers;
using Space.Abstraction.Context;
using Space.DependencyInjection;

// Goal: Compare Space parallel publish using inline dispatcher vs a Task.WhenAll-based dispatcher.
// Rationale: Show the overhead of Task.WhenAll compared to the optimized ValueTask-based inline approach.
[SimpleJob]
[MemoryDiagnoser]
public class NotificationsParallelBench
{
    private ISpace _spaceInline = default!;      // Built-in ParallelNotificationDispatcher path (inline, low alloc)
    private ISpace _spaceWhenAll = default!;     // Custom dispatcher using Task.WhenAll

    private static readonly NP_Event Event = new(5);

    [GlobalSetup(Targets = [nameof(Space_Publish_Parallel_Inline), nameof(Space_Publish_Parallel_TaskWhenAll)])]
    public void Setup()
    {
        // Space with explicit Parallel dispatch (uses optimized inline path in NotificationRegistry)
        var inlineServices = new ServiceCollection();
        inlineServices.AddSpace(opt =>
        {
            opt.ServiceLifetime = ServiceLifetime.Singleton;
            // default remains sequential, we will call Publish with Parallel override per-invocation
        });
        var inlineProvider = inlineServices.BuildServiceProvider();
        _spaceInline = inlineProvider.GetRequiredService<ISpace>();

        // Space with a custom INotificationDispatcher that uses Task.WhenAll
        var whenAllServices = new ServiceCollection();
        whenAllServices.AddSingleton<INotificationDispatcher, TaskWhenAllNotificationDispatcher>();
        whenAllServices.AddSpace(opt =>
        {
            opt.ServiceLifetime = ServiceLifetime.Singleton;
        });
        var whenAllProvider = whenAllServices.BuildServiceProvider();
        _spaceWhenAll = whenAllProvider.GetRequiredService<ISpace>();

        // Warm-up
        for (int i = 0; i < 5_000; i++)
        {
            _spaceInline.Publish(Event, NotificationDispatchType.Parallel).GetAwaiter().GetResult();
            _spaceWhenAll.Publish(Event).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public ValueTask Space_Publish_Parallel_Inline()
        => _spaceInline.Publish(Event, NotificationDispatchType.Parallel);

    [Benchmark]
    public ValueTask Space_Publish_Parallel_TaskWhenAll()
        => _spaceWhenAll.Publish(Event);
}

// Custom dispatcher that mimics the classic Task.WhenAll fan-out for comparison purposes.
public sealed class TaskWhenAllNotificationDispatcher : INotificationDispatcher
{
    public ValueTask DispatchAsync<TRequest>(IEnumerable<System.Func<NotificationContext<TRequest>, ValueTask>> handlers, NotificationContext<TRequest> ctx)
    {
        // Build Task[] and await Task.WhenAll
        var tasks = handlers.Select(h => h(ctx).AsTask()).ToArray();
        if (tasks.Length == 0)
            return default;

        return Await(tasks);

        static async ValueTask Await(Task[] t)
        {
            await Task.WhenAll(t).ConfigureAwait(false);
        }
    }
}

// Dummy event to avoid type name collisions with other benchmarks.
public readonly record struct NP_Event(int Id);
