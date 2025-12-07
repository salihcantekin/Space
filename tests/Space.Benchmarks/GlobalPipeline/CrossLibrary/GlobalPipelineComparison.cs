using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpaceAbstraction = Space.Abstraction;
using Space.DependencyInjection;

namespace Space.Benchmarks.GlobalPipeline.CrossLibrary;

/// <summary>
/// Cross-library benchmark comparing global pipeline/behavior implementations:
/// - Space: GlobalPipeline with compile-time source generation
/// - MediatR: IPipelineBehavior with runtime reflection
/// - Mediator: IPipelineBehavior with source generation
/// 
/// All three implementations perform the same operations:
/// 1. Logging (before/after handler)
/// 2. Validation (check request validity)
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 5, iterationCount: 20)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class GlobalPipelineComparisonBenchmark
{
    private SpaceAbstraction.ISpace space;
    private IMediator mediatR;
    private Mediator.IMediator mediator;

    // ============================================
    // SPACE IMPLEMENTATION
    // ============================================
    public record SpaceReq(int Value) : SpaceAbstraction.Contracts.IRequest<SpaceRes>;
    public record SpaceRes(int Result);

    public class SpaceHandler
    {
        [SpaceAbstraction.Attributes.Handle]
        public ValueTask<SpaceRes> Handle(SpaceAbstraction.Context.HandlerContext<SpaceReq> ctx)
            => ValueTask.FromResult(new SpaceRes(ctx.Request.Value * 2));
    }

    public class SpaceLoggingPipeline
    {
        [SpaceAbstraction.Attributes.GlobalPipeline(Order = 5, ExecutionStage = SpaceAbstraction.Attributes.GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TResponse> Log<TRequest, TResponse>(
            SpaceAbstraction.Context.PipelineContext<TRequest> ctx,
            SpaceAbstraction.Context.PipelineDelegate<TRequest, TResponse> next)
            where TRequest : notnull
            where TResponse : notnull
        {
            _ = ctx.Request.GetType().Name;
            var response = await next(ctx);
            _ = response.GetType().Name;
            return response;
        }
    }

    public class SpaceValidationPipeline
    {
        [SpaceAbstraction.Attributes.GlobalPipeline(Order = 10, ExecutionStage = SpaceAbstraction.Attributes.GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TResponse> Validate<TRequest, TResponse>(
            SpaceAbstraction.Context.PipelineContext<TRequest> ctx,
            SpaceAbstraction.Context.PipelineDelegate<TRequest, TResponse> next)
            where TRequest : notnull
            where TResponse : notnull
        {
            if (ctx.Request is SpaceReq req && req.Value < 0)
                throw new System.InvalidOperationException("Invalid");
            return await next(ctx);
        }
    }

    // ============================================
    // MEDIATR IMPLEMENTATION
    // ============================================
    public record MediatRReq(int Value) : MediatR.IRequest<MediatRRes>;
    public record MediatRRes(int Result);

    public class MediatRHandler : IRequestHandler<MediatRReq, MediatRRes>
    {
        public Task<MediatRRes> Handle(MediatRReq request, CancellationToken cancellationToken)
            => Task.FromResult(new MediatRRes(request.Value * 2));
    }

    public class MediatRLoggingBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : MediatR.IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            _ = request.GetType().Name;
            var response = await next();
            _ = response.GetType().Name;
            return response;
        }
    }

    public class MediatRValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
        where TRequest : MediatR.IRequest<TResponse>
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            if (request is MediatRReq req && req.Value < 0)
                throw new System.InvalidOperationException("Invalid");
            return await next();
        }
    }

    // ============================================
    // MEDIATOR (MICROSOFT) IMPLEMENTATION
    // ============================================
    public record MediatorReq(int Value) : Mediator.IRequest<MediatorRes>;
    public record MediatorRes(int Result);

    public class MediatorHandler : Mediator.IRequestHandler<MediatorReq, MediatorRes>
    {
        public ValueTask<MediatorRes> Handle(MediatorReq request, CancellationToken cancellationToken)
            => ValueTask.FromResult(new MediatorRes(request.Value * 2));
    }

    public class MediatorLoggingBehavior<TMessage, TResponse> : Mediator.IPipelineBehavior<TMessage, TResponse>
        where TMessage : Mediator.IMessage
    {
        public async ValueTask<TResponse> Handle(TMessage message, Mediator.MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            _ = message.GetType().Name;
            var response = await next(message, cancellationToken);
            _ = response.GetType().Name;
            return response;
        }
    }

    public class MediatorValidationBehavior<TMessage, TResponse> : Mediator.IPipelineBehavior<TMessage, TResponse>
        where TMessage : Mediator.IMessage
    {
        public async ValueTask<TResponse> Handle(TMessage message, Mediator.MessageHandlerDelegate<TMessage, TResponse> next, CancellationToken cancellationToken)
        {
            if (message is MediatorReq req && req.Value < 0)
                throw new System.InvalidOperationException("Invalid");
            return await next(message, cancellationToken);
        }
    }

    private SpaceReq spaceReq;
    private MediatRReq mediatRReq;
    private MediatorReq mediatorReq;

    [GlobalSetup]
    public void Setup()
    {
        // Space setup
        var spaceServices = new ServiceCollection();
        spaceServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spaceSp = spaceServices.BuildServiceProvider();
        space = spaceSp.GetRequiredService<SpaceAbstraction.ISpace>();

        // MediatR setup
        var mediatRServices = new ServiceCollection();
        mediatRServices.AddMediatR(typeof(GlobalPipelineComparisonBenchmark).Assembly);
        mediatRServices.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatRLoggingBehavior<,>));
        mediatRServices.AddTransient(typeof(IPipelineBehavior<,>), typeof(MediatRValidationBehavior<,>));
        var mediatRSp = mediatRServices.BuildServiceProvider();
        mediatR = mediatRSp.GetRequiredService<IMediator>();

        // Mediator setup
        var mediatorServices = new ServiceCollection();
        mediatorServices.AddMediator(opts => opts.ServiceLifetime = ServiceLifetime.Singleton);
        mediatorServices.AddSingleton(typeof(Mediator.IPipelineBehavior<,>), typeof(MediatorLoggingBehavior<,>));
        mediatorServices.AddSingleton(typeof(Mediator.IPipelineBehavior<,>), typeof(MediatorValidationBehavior<,>));
        var mediatorSp = mediatorServices.BuildServiceProvider();
        mediator = mediatorSp.GetRequiredService<Mediator.IMediator>();

        spaceReq = new SpaceReq(42);
        mediatRReq = new MediatRReq(42);
        mediatorReq = new MediatorReq(42);

        // Warm-up
        for (int i = 0; i < 5_000; i++)
        {
            _ = space.Send<SpaceReq, SpaceRes>(spaceReq).GetAwaiter().GetResult();
            _ = mediatR.Send(mediatRReq).GetAwaiter().GetResult();
            _ = mediator.Send(mediatorReq).GetAwaiter().GetResult();
        }
    }

    [Benchmark(Baseline = true, Description = "Space GlobalPipeline")]
    public ValueTask<SpaceRes> Space_GlobalPipeline()
        => space.Send<SpaceReq, SpaceRes>(spaceReq);

    [Benchmark(Description = "MediatR Behavior")]
    public Task<MediatRRes> MediatR_PipelineBehavior()
        => mediatR.Send(mediatRReq);

    [Benchmark(Description = "Mediator Behavior")]
    public ValueTask<MediatorRes> Mediator_PipelineBehavior()
        => mediator.Send(mediatorReq);
}
