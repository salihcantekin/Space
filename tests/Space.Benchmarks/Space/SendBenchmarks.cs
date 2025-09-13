using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Benchmarks.Space;

[MemoryDiagnoser]
[SimpleJob(RuntimeMoniker.Net80, launchCount: 1, warmupCount: 5, iterationCount: 15)]
public class SendBenchmarks
{
    private ISpace space;

    // Simple request/response
    public record Req(int Id) : IRequest<Res>;
    public record Res(int Value);

    // Two handlers for same Req/Res to compare named vs unnamed selection
    public class BenchHandlers
    {
        [Handle] // unnamed
        public ValueTask<Res> Default(HandlerContext<Req> ctx)
            => ValueTask.FromResult(new Res(ctx.Request.Id + 1));

        [Handle(Name = "Named")] // named variant
        public ValueTask<Res> Named(HandlerContext<Req> ctx)
            => ValueTask.FromResult(new Res(ctx.Request.Id + 2));
    }

    private Req req;
    private IRequest<Res> ireq;
    private object oreq;

    [GlobalSetup]
    public void Setup()
    {
        var services = new ServiceCollection();
        // Use Singleton to exercise fast path (no scopes, no DI resolution in hot path)
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        space = sp.GetRequiredService<ISpace>();

        // Force handler type creation/registration
        _ = sp.GetRequiredService<BenchHandlers>();

        req = new Req(1);
        ireq = req;
        oreq = req;

        int counter = 2_000;

        while (counter-- > 0)
        {
            // Warm up caches (JIT + EntryCache + GenericDispatcherCache etc.)
            _ = space.Send<Req, Res>(req).GetAwaiter().GetResult();
            _ = space.Send<Req, Res>(req, name: "Named").GetAwaiter().GetResult();
            _ = space.Send<Res>(ireq).GetAwaiter().GetResult();
            _ = space.Send<Res>(ireq, name: "Named").GetAwaiter().GetResult();
            _ = space.Send<Res>(oreq).GetAwaiter().GetResult();
            _ = space.Send<Res>(oreq, name: "Named").GetAwaiter().GetResult();
        }
    }

    // Typed generic (unnamed)
    [Benchmark(Baseline = true)]
    public ValueTask<Res> Typed_Unnamed()
        => space.Send<Req, Res>(req);

    // Typed generic (named)
    [Benchmark]
    public ValueTask<Res> Typed_Named()
        => space.Send<Req, Res>(req, name: "Named");

    // IRequest overload (unnamed)
    [Benchmark]
    public ValueTask<Res> IRequest_Unnamed()
        => space.Send<Res>(ireq);

    // IRequest overload (named)
    [Benchmark]
    public ValueTask<Res> IRequest_Named()
        => space.Send<Res>(ireq, name: "Named");

    // Object-based send (unnamed)
    [Benchmark]
    public ValueTask<Res> Object_Unnamed()
        => space.Send<Res>(oreq);

    // Object-based send (named)
    [Benchmark]
    public ValueTask<Res> Object_Named()
        => space.Send<Res>(oreq, name: "Named");
}
