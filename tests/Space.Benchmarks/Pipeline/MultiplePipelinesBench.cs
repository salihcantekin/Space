using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mediator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpaceAbstraction = Space.Abstraction;
using Space.DependencyInjection;

namespace Space.Benchmarks.Pipeline;

/// <summary>
/// Benchmark comparing performance with multiple pipelines/behaviors (2 and 3).
/// Tests the overhead of pipeline chain composition across libraries.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class MultiplePipelinesBench
{
    private SpaceAbstraction.ISpace space2Pipes = default!;
    private SpaceAbstraction.ISpace space3Pipes = default!;
    private Mediator.IMediator mediator2 = default!;
    private Mediator.IMediator mediator3 = default!;
    private MediatR.IMediator mediatR2 = default!;
    private MediatR.IMediator mediatR3 = default!;

    // 2-Pipeline requests
    private static readonly MP2_SpaceRequest Space2Req = new(10);
    private static readonly MP2_MediatorRequest Mediator2Req = new(10);
    private static readonly MP2_MediatRRequest MediatR2Req = new(10);

    // 3-Pipeline requests
    private static readonly MP3_SpaceRequest Space3Req = new(10);
    private static readonly MP3_MediatorRequest Mediator3Req = new(10);
    private static readonly MP3_MediatRRequest MediatR3Req = new(10);

    [GlobalSetup]
    public void Setup()
    {
        // Space with 2 pipelines
        var sp2Services = new ServiceCollection();
        sp2Services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp2Provider = sp2Services.BuildServiceProvider();
        space2Pipes = sp2Provider.GetRequiredService<SpaceAbstraction.ISpace>();

        // Space with 3 pipelines (same provider, different handler)
        space3Pipes = space2Pipes;

        // Mediator with 2 behaviors
        var med2Services = new ServiceCollection();
        med2Services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        med2Services.AddSingleton(typeof(Mediator.IPipelineBehavior<MP2_MediatorRequest, MP_Response>), typeof(MP2_MediatorBehavior1));
        med2Services.AddSingleton(typeof(Mediator.IPipelineBehavior<MP2_MediatorRequest, MP_Response>), typeof(MP2_MediatorBehavior2));
        var med2Provider = med2Services.BuildServiceProvider();
        mediator2 = med2Provider.GetRequiredService<Mediator.IMediator>();

        // Mediator with 3 behaviors
        var med3Services = new ServiceCollection();
        med3Services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        med3Services.AddSingleton(typeof(Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>), typeof(MP3_MediatorBehavior1));
        med3Services.AddSingleton(typeof(Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>), typeof(MP3_MediatorBehavior2));
        med3Services.AddSingleton(typeof(Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>), typeof(MP3_MediatorBehavior3));
        var med3Provider = med3Services.BuildServiceProvider();
        mediator3 = med3Provider.GetRequiredService<Mediator.IMediator>();

        // MediatR with 2 behaviors
        var mr2Services = new ServiceCollection();
        mr2Services.AddMediatR(Assembly.GetExecutingAssembly());
        mr2Services.AddSingleton(typeof(MediatR.IPipelineBehavior<MP2_MediatRRequest, MP_Response>), typeof(MP2_MediatRBehavior1));
        mr2Services.AddSingleton(typeof(MediatR.IPipelineBehavior<MP2_MediatRRequest, MP_Response>), typeof(MP2_MediatRBehavior2));
        var mr2Provider = mr2Services.BuildServiceProvider();
        mediatR2 = mr2Provider.GetRequiredService<MediatR.IMediator>();

        // MediatR with 3 behaviors
        var mr3Services = new ServiceCollection();
        mr3Services.AddMediatR(Assembly.GetExecutingAssembly());
        mr3Services.AddSingleton(typeof(MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>), typeof(MP3_MediatRBehavior1));
        mr3Services.AddSingleton(typeof(MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>), typeof(MP3_MediatRBehavior2));
        mr3Services.AddSingleton(typeof(MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>), typeof(MP3_MediatRBehavior3));
        var mr3Provider = mr3Services.BuildServiceProvider();
        mediatR3 = mr3Provider.GetRequiredService<MediatR.IMediator>();

        // Warm-up
        for (int i = 0; i < 5_000; i++)
        {
            _ = space2Pipes.Send<MP2_SpaceRequest, MP_Response>(Space2Req).GetAwaiter().GetResult();
            _ = space3Pipes.Send<MP3_SpaceRequest, MP_Response>(Space3Req).GetAwaiter().GetResult();
            _ = mediator2.Send(Mediator2Req).GetAwaiter().GetResult();
            _ = mediator3.Send(Mediator3Req).GetAwaiter().GetResult();
            _ = mediatR2.Send(MediatR2Req).GetAwaiter().GetResult();
            _ = mediatR3.Send(MediatR3Req).GetAwaiter().GetResult();
        }
    }

    // ============================================
    // 2 PIPELINES
    // ============================================

    [Benchmark(Description = "Space (2 pipes)")]
    public ValueTask<MP_Response> Space_2Pipelines()
        => space2Pipes.Send<MP2_SpaceRequest, MP_Response>(Space2Req);

    [Benchmark(Description = "Mediator (2 behaviors)")]
    public ValueTask<MP_Response> Mediator_2Behaviors()
        => mediator2.Send(Mediator2Req);

    [Benchmark(Description = "MediatR (2 behaviors)")]
    public Task<MP_Response> MediatR_2Behaviors()
        => mediatR2.Send(MediatR2Req);

    // ============================================
    // 3 PIPELINES
    // ============================================

    [Benchmark(Description = "Space (3 pipes)")]
    public ValueTask<MP_Response> Space_3Pipelines()
        => space3Pipes.Send<MP3_SpaceRequest, MP_Response>(Space3Req);

    [Benchmark(Description = "Mediator (3 behaviors)")]
    public ValueTask<MP_Response> Mediator_3Behaviors()
        => mediator3.Send(Mediator3Req);

    [Benchmark(Description = "MediatR (3 behaviors)")]
    public Task<MP_Response> MediatR_3Behaviors()
        => mediatR3.Send(MediatR3Req);
}

// ============================================
// SHARED RESPONSE
// ============================================
public readonly record struct MP_Response(int Value);

// ============================================
// 2-PIPELINE SPACE IMPLEMENTATION
// ============================================
public readonly record struct MP2_SpaceRequest(int Id) : SpaceAbstraction.Contracts.IRequest<MP_Response>;

public sealed class MP2_SpaceHandlers
{
    [SpaceAbstraction.Attributes.Handle]
    public ValueTask<MP_Response> Handle(SpaceAbstraction.Context.HandlerContext<MP2_SpaceRequest> ctx)
        => ValueTask.FromResult(new MP_Response(ctx.Request.Id * 2));

    [SpaceAbstraction.Attributes.Pipeline(Order = 1)]
    public ValueTask<MP_Response> Pipeline1(SpaceAbstraction.Context.PipelineContext<MP2_SpaceRequest> ctx, SpaceAbstraction.Context.PipelineDelegate<MP2_SpaceRequest, MP_Response> next)
        => next(ctx);

    [SpaceAbstraction.Attributes.Pipeline(Order = 2)]
    public ValueTask<MP_Response> Pipeline2(SpaceAbstraction.Context.PipelineContext<MP2_SpaceRequest> ctx, SpaceAbstraction.Context.PipelineDelegate<MP2_SpaceRequest, MP_Response> next)
        => next(ctx);
}

// ============================================
// 3-PIPELINE SPACE IMPLEMENTATION
// ============================================
public readonly record struct MP3_SpaceRequest(int Id) : SpaceAbstraction.Contracts.IRequest<MP_Response>;

public sealed class MP3_SpaceHandlers
{
    [SpaceAbstraction.Attributes.Handle]
    public ValueTask<MP_Response> Handle(SpaceAbstraction.Context.HandlerContext<MP3_SpaceRequest> ctx)
        => ValueTask.FromResult(new MP_Response(ctx.Request.Id * 2));

    [SpaceAbstraction.Attributes.Pipeline(Order = 1)]
    public ValueTask<MP_Response> Pipeline1(SpaceAbstraction.Context.PipelineContext<MP3_SpaceRequest> ctx, SpaceAbstraction.Context.PipelineDelegate<MP3_SpaceRequest, MP_Response> next)
        => next(ctx);

    [SpaceAbstraction.Attributes.Pipeline(Order = 2)]
    public ValueTask<MP_Response> Pipeline2(SpaceAbstraction.Context.PipelineContext<MP3_SpaceRequest> ctx, SpaceAbstraction.Context.PipelineDelegate<MP3_SpaceRequest, MP_Response> next)
        => next(ctx);

    [SpaceAbstraction.Attributes.Pipeline(Order = 3)]
    public ValueTask<MP_Response> Pipeline3(SpaceAbstraction.Context.PipelineContext<MP3_SpaceRequest> ctx, SpaceAbstraction.Context.PipelineDelegate<MP3_SpaceRequest, MP_Response> next)
        => next(ctx);
}

// ============================================
// 2-PIPELINE MEDIATOR IMPLEMENTATION
// ============================================
public readonly record struct MP2_MediatorRequest(int Id) : Mediator.IRequest<MP_Response>;

public sealed class MP2_MediatorHandler : Mediator.IRequestHandler<MP2_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP2_MediatorRequest request, CancellationToken ct)
        => ValueTask.FromResult(new MP_Response(request.Id * 2));
}

public sealed class MP2_MediatorBehavior1 : Mediator.IPipelineBehavior<MP2_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP2_MediatorRequest msg, MessageHandlerDelegate<MP2_MediatorRequest, MP_Response> next, CancellationToken ct)
        => next(msg, ct);
}

public sealed class MP2_MediatorBehavior2 : Mediator.IPipelineBehavior<MP2_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP2_MediatorRequest msg, MessageHandlerDelegate<MP2_MediatorRequest, MP_Response> next, CancellationToken ct)
        => next(msg, ct);
}

// ============================================
// 3-PIPELINE MEDIATOR IMPLEMENTATION
// ============================================
public readonly record struct MP3_MediatorRequest(int Id) : Mediator.IRequest<MP_Response>;

public sealed class MP3_MediatorHandler : Mediator.IRequestHandler<MP3_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP3_MediatorRequest request, CancellationToken ct)
        => ValueTask.FromResult(new MP_Response(request.Id * 2));
}

public sealed class MP3_MediatorBehavior1 : Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP3_MediatorRequest msg, MessageHandlerDelegate<MP3_MediatorRequest, MP_Response> next, CancellationToken ct)
        => next(msg, ct);
}

public sealed class MP3_MediatorBehavior2 : Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP3_MediatorRequest msg, MessageHandlerDelegate<MP3_MediatorRequest, MP_Response> next, CancellationToken ct)
        => next(msg, ct);
}

public sealed class MP3_MediatorBehavior3 : Mediator.IPipelineBehavior<MP3_MediatorRequest, MP_Response>
{
    public ValueTask<MP_Response> Handle(MP3_MediatorRequest msg, MessageHandlerDelegate<MP3_MediatorRequest, MP_Response> next, CancellationToken ct)
        => next(msg, ct);
}

// ============================================
// 2-PIPELINE MEDIATR IMPLEMENTATION
// ============================================
public readonly record struct MP2_MediatRRequest(int Id) : MediatR.IRequest<MP_Response>;

public sealed class MP2_MediatRHandler : MediatR.IRequestHandler<MP2_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP2_MediatRRequest request, CancellationToken ct)
        => Task.FromResult(new MP_Response(request.Id * 2));
}

public sealed class MP2_MediatRBehavior1 : MediatR.IPipelineBehavior<MP2_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP2_MediatRRequest request, RequestHandlerDelegate<MP_Response> next, CancellationToken ct)
        => next();
}

public sealed class MP2_MediatRBehavior2 : MediatR.IPipelineBehavior<MP2_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP2_MediatRRequest request, RequestHandlerDelegate<MP_Response> next, CancellationToken ct)
        => next();
}

// ============================================
// 3-PIPELINE MEDIATR IMPLEMENTATION
// ============================================
public readonly record struct MP3_MediatRRequest(int Id) : MediatR.IRequest<MP_Response>;

public sealed class MP3_MediatRHandler : MediatR.IRequestHandler<MP3_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP3_MediatRRequest request, CancellationToken ct)
        => Task.FromResult(new MP_Response(request.Id * 2));
}

public sealed class MP3_MediatRBehavior1 : MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP3_MediatRRequest request, RequestHandlerDelegate<MP_Response> next, CancellationToken ct)
        => next();
}

public sealed class MP3_MediatRBehavior2 : MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP3_MediatRRequest request, RequestHandlerDelegate<MP_Response> next, CancellationToken ct)
        => next();
}

public sealed class MP3_MediatRBehavior3 : MediatR.IPipelineBehavior<MP3_MediatRRequest, MP_Response>
{
    public Task<MP_Response> Handle(MP3_MediatRRequest request, RequestHandlerDelegate<MP_Response> next, CancellationToken ct)
        => next();
}
