using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Mediator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SpaceAbstraction = Space.Abstraction;
using Space.DependencyInjection;

namespace Space.Benchmarks.Handler;

/// <summary>
/// Benchmark comparing object-based dispatch (runtime type resolution).
/// Tests scenarios where the request type is not known at compile time.
/// </summary>
[SimpleJob]
[MemoryDiagnoser]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
public class ObjectSendBench
{
    private SpaceAbstraction.ISpace space = default!;
    private Mediator.IMediator mediator = default!;
    private MediatR.IMediator mediatR = default!;

    // Object references (compile-time type is object/interface)
    private object spaceReqObject = default!;
    private object mediatorReqObject = default!;
    private object mediatRReqObject = default!;

    // Typed references for comparison
    private static readonly OS_SpaceRequest SpaceReqTyped = new(42);
    private static readonly OS_MediatorRequest MediatorReqTyped = new(42);
    private static readonly OS_MediatRRequest MediatRReqTyped = new(42);

    [GlobalSetup]
    public void Setup()
    {
        // Space
        var spServices = new ServiceCollection();
        spServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spProvider = spServices.BuildServiceProvider();
        space = spProvider.GetRequiredService<SpaceAbstraction.ISpace>();

        // Mediator
        var medServices = new ServiceCollection();
        medServices.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var medProvider = medServices.BuildServiceProvider();
        mediator = medProvider.GetRequiredService<Mediator.IMediator>();

        // MediatR
        var mrServices = new ServiceCollection();
        mrServices.AddMediatR(Assembly.GetExecutingAssembly());
        var mrProvider = mrServices.BuildServiceProvider();
        mediatR = mrProvider.GetRequiredService<MediatR.IMediator>();

        // Create object references (simulating runtime type resolution)
        spaceReqObject = new OS_SpaceRequest(42);
        mediatorReqObject = new OS_MediatorRequest(42);
        mediatRReqObject = new OS_MediatRRequest(42);

        // Warm-up typed
        for (int i = 0; i < 10_000; i++)
        {
            _ = space.Send<OS_SpaceRequest, OS_Response>(SpaceReqTyped).GetAwaiter().GetResult();
            _ = mediator.Send(MediatorReqTyped).GetAwaiter().GetResult();
            _ = mediatR.Send(MediatRReqTyped).GetAwaiter().GetResult();
        }

        // Warm-up object
        for (int i = 0; i < 10_000; i++)
        {
            _ = space.Send<OS_Response>(spaceReqObject).GetAwaiter().GetResult();
            _ = mediator.Send(mediatorReqObject).GetAwaiter().GetResult();
            _ = mediatR.Send(mediatRReqObject).GetAwaiter().GetResult();
        }
    }

    // ============================================
    // TYPED DISPATCH (Baseline)
    // ============================================

    [Benchmark(Baseline = true, Description = "Space Typed")]
    public ValueTask<OS_Response> Space_Typed()
        => space.Send<OS_SpaceRequest, OS_Response>(SpaceReqTyped);

    [Benchmark(Description = "Mediator Typed")]
    public ValueTask<OS_Response> Mediator_Typed()
        => mediator.Send(MediatorReqTyped);

    [Benchmark(Description = "MediatR Typed")]
    public Task<OS_Response> MediatR_Typed()
        => mediatR.Send(MediatRReqTyped);

    // ============================================
    // OBJECT DISPATCH (Runtime type resolution)
    // ============================================

    [Benchmark(Description = "Space Object")]
    public ValueTask<OS_Response> Space_Object()
        => space.Send<OS_Response>(spaceReqObject);

    [Benchmark(Description = "Mediator Object")]
    public async ValueTask<OS_Response> Mediator_Object()
    {
        // Mediator uses Send(object) internally
        var result = await mediator.Send(mediatorReqObject);
        return (OS_Response)result!;
    }

    [Benchmark(Description = "MediatR Object")]
    public async Task<OS_Response> MediatR_Object()
    {
        // MediatR uses Send(object) internally
        var result = await mediatR.Send(mediatRReqObject);
        return (OS_Response)result!;
    }
}

// ============================================
// SPACE IMPLEMENTATION
// ============================================
public readonly record struct OS_SpaceRequest(int Id) : SpaceAbstraction.Contracts.IRequest<OS_Response>;

public sealed class OS_SpaceHandler
{
    [SpaceAbstraction.Attributes.Handle]
    public ValueTask<OS_Response> Handle(SpaceAbstraction.Context.HandlerContext<OS_SpaceRequest> ctx)
        => ValueTask.FromResult(new OS_Response(ctx.Request.Id + 1));
}

// ============================================
// MEDIATOR IMPLEMENTATION
// ============================================
public readonly record struct OS_MediatorRequest(int Id) : Mediator.IRequest<OS_Response>;

public sealed class OS_MediatorHandler : Mediator.IRequestHandler<OS_MediatorRequest, OS_Response>
{
    public ValueTask<OS_Response> Handle(OS_MediatorRequest request, CancellationToken ct)
        => ValueTask.FromResult(new OS_Response(request.Id + 1));
}

// ============================================
// MEDIATR IMPLEMENTATION
// ============================================
public readonly record struct OS_MediatRRequest(int Id) : MediatR.IRequest<OS_Response>;

public sealed class OS_MediatRHandler : MediatR.IRequestHandler<OS_MediatRRequest, OS_Response>
{
    public Task<OS_Response> Handle(OS_MediatRRequest request, CancellationToken ct)
        => Task.FromResult(new OS_Response(request.Id + 1));
}

// ============================================
// SHARED RESPONSE
// ============================================
public readonly record struct OS_Response(int Value);
