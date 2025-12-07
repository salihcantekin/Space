using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Benchmarks.GlobalPipeline;

/// <summary>
/// Benchmark to ensure that when GlobalPipeline feature is not used,
/// performance remains identical to the baseline (no global pipeline overhead).
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 5, iterationCount: 15)]
public class GlobalPipelineOverheadBenchmark
{
    private ISpace spaceWithoutGlobalPipeline;
    private ISpace spaceWithGlobalPipeline;

    // Simple request/response
    public record BenchReq(int Id) : IRequest<BenchRes>;
    public record BenchRes(int Value);

    // Handler without global pipeline
    public class HandlerWithoutGlobalPipeline
    {
        [Handle]
        public ValueTask<BenchRes> Handle(HandlerContext<BenchReq> ctx)
            => ValueTask.FromResult(new BenchRes(ctx.Request.Id + 1));
    }

    // Handler with global pipeline (different request type to avoid conflicts)
    public record BenchReqWithGP(int Id) : IRequest<BenchResWithGP>;
    public record BenchResWithGP(int Value);

    public class HandlerWithGlobalPipeline
    {
        [Handle]
        public ValueTask<BenchResWithGP> Handle(HandlerContext<BenchReqWithGP> ctx)
            => ValueTask.FromResult(new BenchResWithGP(ctx.Request.Id + 1));
    }

    public class SampleGlobalPipeline
    {
        [GlobalPipeline(Order = 100)]
        public async ValueTask<BenchResWithGP> GlobalPipe(PipelineContext<BenchReqWithGP> ctx, PipelineDelegate<BenchReqWithGP, BenchResWithGP> next)
        {
            var res = await next(ctx);
            return new BenchResWithGP(res.Value + 1); // Simple transformation
        }
    }

    private BenchReq reqWithout;
    private BenchReqWithGP reqWith;

    [GlobalSetup]
    public void Setup()
    {
        // Setup without global pipeline
        var servicesWithout = new ServiceCollection();
        servicesWithout.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spWithout = servicesWithout.BuildServiceProvider();
        spaceWithoutGlobalPipeline = spWithout.GetRequiredService<ISpace>();

        // Setup with global pipeline
        var servicesWith = new ServiceCollection();
        servicesWith.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spWith = servicesWith.BuildServiceProvider();
        spaceWithGlobalPipeline = spWith.GetRequiredService<ISpace>();

        reqWithout = new BenchReq(1);
        reqWith = new BenchReqWithGP(1);

        // Warm-up
        for (int i = 0; i < 10_000; i++)
        {
            _ = spaceWithoutGlobalPipeline.Send<BenchReq, BenchRes>(reqWithout).GetAwaiter().GetResult();
            _ = spaceWithGlobalPipeline.Send<BenchReqWithGP, BenchResWithGP>(reqWith).GetAwaiter().GetResult();
        }
    }

    /// <summary>
    /// Baseline: Handler without any global pipeline in the system.
    /// This should be the fastest and serve as the baseline.
    /// </summary>
    [Benchmark(Baseline = true)]
    public ValueTask<BenchRes> WithoutGlobalPipeline()
        => spaceWithoutGlobalPipeline.Send<BenchReq, BenchRes>(reqWithout);

    /// <summary>
    /// Handler with a global pipeline registered and executing.
    /// Should have minimal overhead compared to baseline.
    /// </summary>
    [Benchmark]
    public ValueTask<BenchResWithGP> WithGlobalPipeline()
        => spaceWithGlobalPipeline.Send<BenchReqWithGP, BenchResWithGP>(reqWith);
}

/// <summary>
/// Benchmark to measure overhead of multiple global pipelines.
/// Compares single global pipeline vs. multiple global pipelines with different ExecutionStages.
/// </summary>
[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 5, iterationCount: 15)]
public class MultipleGlobalPipelinesBenchmark
{
    private ISpace spaceSingleGP;
    private ISpace spaceMultipleGP;

    public record SingleGPReq(int Id) : IRequest<SingleGPRes>;
    public record SingleGPRes(int Value);

    public record MultiGPReq(int Id) : IRequest<MultiGPRes>;
    public record MultiGPRes(int Value);

    public class SingleGPHandler
    {
        [Handle]
        public ValueTask<SingleGPRes> Handle(HandlerContext<SingleGPReq> ctx)
            => ValueTask.FromResult(new SingleGPRes(ctx.Request.Id + 1));
    }

    public class SingleGlobalPipeline
    {
        [GlobalPipeline]
        public async ValueTask<SingleGPRes> GP(PipelineContext<SingleGPReq> ctx, PipelineDelegate<SingleGPReq, SingleGPRes> next)
        {
            var res = await next(ctx);
            return new SingleGPRes(res.Value + 1);
        }
    }

    public class MultiGPHandler
    {
        [Handle]
        public ValueTask<MultiGPRes> Handle(HandlerContext<MultiGPReq> ctx)
            => ValueTask.FromResult(new MultiGPRes(ctx.Request.Id + 1));
    }

    public class MultipleGlobalPipelines
    {
        [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<MultiGPRes> GP1(PipelineContext<MultiGPReq> ctx, PipelineDelegate<MultiGPReq, MultiGPRes> next)
        {
            var res = await next(ctx);
            return new MultiGPRes(res.Value + 1);
        }

        [GlobalPipeline(Order = 20, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<MultiGPRes> GP2(PipelineContext<MultiGPReq> ctx, PipelineDelegate<MultiGPReq, MultiGPRes> next)
        {
            var res = await next(ctx);
            return new MultiGPRes(res.Value + 1);
        }

        [GlobalPipeline(Order = 30, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandlerInner)]
        public async ValueTask<MultiGPRes> GP3(PipelineContext<MultiGPReq> ctx, PipelineDelegate<MultiGPReq, MultiGPRes> next)
        {
            var res = await next(ctx);
            return new MultiGPRes(res.Value + 1);
        }
    }

    private SingleGPReq singleReq;
    private MultiGPReq multiReq;

    [GlobalSetup]
    public void Setup()
    {
        // Single global pipeline
        var servicesSingle = new ServiceCollection();
        servicesSingle.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spSingle = servicesSingle.BuildServiceProvider();
        spaceSingleGP = spSingle.GetRequiredService<ISpace>();

        // Multiple global pipelines
        var servicesMulti = new ServiceCollection();
        servicesMulti.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spMulti = servicesMulti.BuildServiceProvider();
        spaceMultipleGP = spMulti.GetRequiredService<ISpace>();

        singleReq = new SingleGPReq(1);
        multiReq = new MultiGPReq(1);

        // Warm-up
        for (int i = 0; i < 10_000; i++)
        {
            _ = spaceSingleGP.Send<SingleGPReq, SingleGPRes>(singleReq).GetAwaiter().GetResult();
            _ = spaceMultipleGP.Send<MultiGPReq, MultiGPRes>(multiReq).GetAwaiter().GetResult();
        }
    }

    [Benchmark(Baseline = true)]
    public ValueTask<SingleGPRes> Single_GlobalPipeline()
        => spaceSingleGP.Send<SingleGPReq, SingleGPRes>(singleReq);

    [Benchmark]
    public ValueTask<MultiGPRes> Multiple_GlobalPipelines()
        => spaceMultipleGP.Send<MultiGPReq, MultiGPRes>(multiReq);
}
