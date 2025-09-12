using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mediator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;

// Goal: Measure publish performance with two subscribers using sequential dispatch.
// Rationale: All three libraries support notifications; we align the dispatch strategy to sequential for fairness.
[SimpleJob]
[MemoryDiagnoser]
public class NotificationsSequentialBench
{
    private ISpace space = default!;
    private Mediator.IPublisher mediatorPublisher = default!;
    private MediatR.IMediator mediatR = default!; // IMediator has Publish

    private static readonly N_Event Event = new(5);

    [GlobalSetup(Targets = [nameof(Space_Publish), nameof(Mediator_Publish), nameof(MediatR_Publish)])]
    public void Setup()
    {
        // Space with sequential notifications
        var spServices = new ServiceCollection();
        spServices.AddSpace(opt =>
        {
            opt.ServiceLifetime = ServiceLifetime.Singleton;
            opt.NotificationDispatchType = NotificationDispatchType.Sequential;
        });

        var spProvider = spServices.BuildServiceProvider();
        space = spProvider.GetRequiredService<ISpace>();

        // Mediator with notifications
        var medServices = new ServiceCollection();
        medServices.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var medProvider = medServices.BuildServiceProvider();
        mediatorPublisher = medProvider.GetRequiredService<Mediator.IPublisher>();

        // MediatR with notifications
        var mrServices = new ServiceCollection();
        mrServices.AddMediatR(Assembly.GetExecutingAssembly());
        var mrProvider = mrServices.BuildServiceProvider();
        mediatR = mrProvider.GetRequiredService<MediatR.IMediator>();

        // Warm-up
        for (int i = 0; i < 5_000; i++)
        {
            space.Publish(Event).GetAwaiter().GetResult();
            mediatorPublisher.Publish(Event, CancellationToken.None).GetAwaiter().GetResult();
            mediatR.Publish(Event).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public ValueTask Space_Publish()
        => space.Publish(Event);

    [Benchmark]
    public ValueTask Mediator_Publish()
        => mediatorPublisher.Publish(Event, CancellationToken.None);

    [Benchmark]
    public Task MediatR_Publish()
        => mediatR.Publish(Event);
}

// Space notifications (two subscribers)
public sealed class N_SpaceNotifications
{
    [Notification]
    public ValueTask OnA(NotificationContext<N_Event> ctx) => ValueTask.CompletedTask;

    [Notification]
    public ValueTask OnB(NotificationContext<N_Event> ctx) => ValueTask.CompletedTask;
}

// Mediator notifications (two subscribers)
public sealed class N_MediatorHandlerA : Mediator.INotificationHandler<N_Event>
{
    public ValueTask Handle(N_Event notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

public sealed class N_MediatorHandlerB : Mediator.INotificationHandler<N_Event>
{
    public ValueTask Handle(N_Event notification, CancellationToken cancellationToken) => ValueTask.CompletedTask;
}

// MediatR notifications (two subscribers)
public sealed class N_MediatRHandlerA : MediatR.INotificationHandler<N_Event>
{
    public Task Handle(N_Event notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class N_MediatRHandlerB : MediatR.INotificationHandler<N_Event>
{
    public Task Handle(N_Event notification, CancellationToken cancellationToken) => Task.CompletedTask;
}

public readonly record struct N_Event(int Id) : Mediator.INotification, MediatR.INotification;
