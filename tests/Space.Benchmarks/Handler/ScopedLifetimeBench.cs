using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpaceAbstraction = Space.Abstraction;
using Space.DependencyInjection;

namespace Space.Benchmarks.Handler;

/// <summary>
/// Benchmark comparing Scoped vs Singleton handler lifetime performance.
/// Scoped handlers require scope creation per request which adds overhead.
/// Note: Mediator library requires root-level resolution, so we only compare Space and MediatR for scoped.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class ScopedLifetimeBench
{
    // Singleton instances
    private SpaceAbstraction.ISpace spaceSingleton = default!;
    private MediatR.IMediator mediatRSingleton = default!;

    // Scoped instances
    private SpaceAbstraction.ISpace spaceScoped = default!;
    private MediatR.IMediator mediatRScoped = default!;

    private static readonly SL_SpaceRequest SpaceReq = new(42);
    private static readonly SL_MediatRRequest MediatRReq = new(42);

    [GlobalSetup]
    public void Setup()
    {
        // ============================================
        // SINGLETON SETUP
        // ============================================

        // Space Singleton
        var spSingletonServices = new ServiceCollection();
        spSingletonServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spSingletonProvider = spSingletonServices.BuildServiceProvider();
        spaceSingleton = spSingletonProvider.GetRequiredService<SpaceAbstraction.ISpace>();

        // MediatR (transient handlers by default)
        var mrSingletonServices = new ServiceCollection();
        mrSingletonServices.AddMediatR(Assembly.GetExecutingAssembly());
        var mrSingletonProvider = mrSingletonServices.BuildServiceProvider();
        mediatRSingleton = mrSingletonProvider.GetRequiredService<MediatR.IMediator>();

        // ============================================
        // SCOPED SETUP
        // ============================================

        // Space Scoped
        var spScopedServices = new ServiceCollection();
        spScopedServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Scoped);
        var spScopedProvider = spScopedServices.BuildServiceProvider();
        spaceScoped = spScopedProvider.GetRequiredService<SpaceAbstraction.ISpace>();

        // MediatR Scoped (same provider - MediatR always uses transient by default)
        mediatRScoped = mediatRSingleton;

        // Warm-up
        for (int i = 0; i < 10_000; i++)
        {
            _ = spaceSingleton.Send<SL_SpaceRequest, SL_Response>(SpaceReq).GetAwaiter().GetResult();
            _ = spaceScoped.Send<SL_SpaceRequest, SL_Response>(SpaceReq).GetAwaiter().GetResult();
            _ = mediatRSingleton.Send(MediatRReq).GetAwaiter().GetResult();
        }
    }

    // ============================================
    // SINGLETON HANDLERS
    // ============================================

    [Benchmark(Baseline = true, Description = "Space Singleton")]
    public ValueTask<SL_Response> Space_Singleton()
        => spaceSingleton.Send<SL_SpaceRequest, SL_Response>(SpaceReq);

    [Benchmark(Description = "MediatR (transient)")]
    public Task<SL_Response> MediatR_Transient()
        => mediatRSingleton.Send(MediatRReq);

    // ============================================
    // SCOPED HANDLERS (Space only - others don't support this pattern well)
    // ============================================

    [Benchmark(Description = "Space Scoped")]
    public ValueTask<SL_Response> Space_Scoped()
        => spaceScoped.Send<SL_SpaceRequest, SL_Response>(SpaceReq);
}

// ============================================
// SPACE IMPLEMENTATION
// ============================================
public readonly record struct SL_SpaceRequest(int Id) : SpaceAbstraction.Contracts.IRequest<SL_Response>;

public sealed class SL_SpaceHandler
{
    [SpaceAbstraction.Attributes.Handle]
    public ValueTask<SL_Response> Handle(SpaceAbstraction.Context.HandlerContext<SL_SpaceRequest> ctx)
        => ValueTask.FromResult(new SL_Response(ctx.Request.Id + 1));
}

// ============================================
// MEDIATR IMPLEMENTATION
// ============================================
public readonly record struct SL_MediatRRequest(int Id) : MediatR.IRequest<SL_Response>;

public sealed class SL_MediatRHandler : MediatR.IRequestHandler<SL_MediatRRequest, SL_Response>
{
    public Task<SL_Response> Handle(SL_MediatRRequest request, CancellationToken ct)
        => Task.FromResult(new SL_Response(request.Id + 1));
}

// ============================================
// SHARED RESPONSE
// ============================================
public readonly record struct SL_Response(int Value);
