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

// Goal: Measure request/response dispatch when a single middleware wraps the handler.
// Rationale: Many real apps have cross-cutting behaviors. This compares pipeline cost uniformly.
[SimpleJob]
[MemoryDiagnoser]
public class HandlerWithPipelineBench
{
    private ISpace space = default!;
    private Mediator.IMediator mediator = default!;
    private MediatR.IMediator mediatR = default!;

    private static readonly HP_SpaceRequest SpaceReq = new(7);
    private static readonly HP_MediatorRequest MediatorReq = new(7);
    private static readonly HP_MediatRRequest MediatRReq = new(7);

    [GlobalSetup(Targets = [nameof(Space_Send_WithPipeline), nameof(Mediator_Send_WithBehavior), nameof(MediatR_Send_WithBehavior)])]
    public void Setup()
    {
        // Space with pipeline discovered by source generator via [Pipeline]
        var spServices = new ServiceCollection();
        spServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spProvider = spServices.BuildServiceProvider();
        space = spProvider.GetRequiredService<ISpace>();

        // Mediator + explicit behavior registration for concrete request/response
        var medServices = new ServiceCollection();
        medServices.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        medServices.AddSingleton(typeof(Mediator.IPipelineBehavior<HP_MediatorRequest, HP_Response>), typeof(HP_MediatorBehavior));
        var medProvider = medServices.BuildServiceProvider();
        mediator = medProvider.GetRequiredService<Mediator.IMediator>();

        // MediatR + explicit behavior registration for concrete request/response
        var mrServices = new ServiceCollection();
        mrServices.AddMediatR(Assembly.GetExecutingAssembly());
        mrServices.AddSingleton(typeof(MediatR.IPipelineBehavior<HP_MediatRRequest, HP_Response>), typeof(HP_MediatRBehavior));
        var mrProvider = mrServices.BuildServiceProvider();
        mediatR = mrProvider.GetRequiredService<MediatR.IMediator>();

        // Warm-up
        for (int i = 0; i < 10_000; i++)
        {
            _ = space.Send<HP_SpaceRequest, HP_Response>(SpaceReq).GetAwaiter().GetResult();
            _ = mediator.Send(MediatorReq).GetAwaiter().GetResult();
            _ = mediatR.Send(MediatRReq).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public async ValueTask<HP_Response> Space_Send_WithPipeline()
        => await space.Send<HP_SpaceRequest, HP_Response>(SpaceReq);

    [Benchmark]
    public async ValueTask<HP_Response> Mediator_Send_WithBehavior()
        => await mediator.Send(MediatorReq);

    [Benchmark]
    public async Task<HP_Response> MediatR_Send_WithBehavior()
        => await mediatR.Send(MediatRReq);
}

// Space: handler + pipeline
public sealed class HP_SpaceHandlers
{
    [Handle]
    public ValueTask<HP_Response> Handle(HandlerContext<HP_SpaceRequest> ctx)
        => ValueTask.FromResult(new HP_Response(ctx.Request.Id * 2));

    [Pipeline]
    public ValueTask<HP_Response> Pipeline(PipelineContext<HP_SpaceRequest> ctx, PipelineDelegate<HP_SpaceRequest, HP_Response> next)
        => next(ctx); // no-op middleware
}

// Mediator: handler + behavior
public sealed class HP_MediatorHandler : Mediator.IRequestHandler<HP_MediatorRequest, HP_Response>
{
    public ValueTask<HP_Response> Handle(HP_MediatorRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(new HP_Response(request.Id * 2));
}

public sealed class HP_MediatorBehavior : Mediator.IPipelineBehavior<HP_MediatorRequest, HP_Response>
{
    public ValueTask<HP_Response> Handle(HP_MediatorRequest message, MessageHandlerDelegate<HP_MediatorRequest, HP_Response> next, CancellationToken cancellationToken)
        => next(message, cancellationToken); // no-op behavior
}

// MediatR: handler + behavior
public sealed class HP_MediatRHandler : MediatR.IRequestHandler<HP_MediatRRequest, HP_Response>
{
    public Task<HP_Response> Handle(HP_MediatRRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new HP_Response(request.Id * 2));
}

public sealed class HP_MediatRBehavior : MediatR.IPipelineBehavior<HP_MediatRRequest, HP_Response>
{
    public Task<HP_Response> Handle(HP_MediatRRequest request, RequestHandlerDelegate<HP_Response> next, CancellationToken cancellationToken)
        => next(); // no-op behavior
}

public readonly record struct HP_SpaceRequest(int Id) : Space.Abstraction.Contracts.IRequest<HP_Response>;
public readonly record struct HP_MediatorRequest(int Id) : Mediator.IRequest<HP_Response>;
public readonly record struct HP_MediatRRequest(int Id) : MediatR.IRequest<HP_Response>;
public readonly record struct HP_Response(int Value);
