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

// Goal: Measure pure request/response dispatch cost without any pipeline/middleware.
// Rationale: This isolates the core send path for each library under identical conditions.
[SimpleJob]
[MemoryDiagnoser]
public class HandlerOnlyBench
{
    private ISpace _space = default!;
    private Mediator.IMediator _mediator = default!;
    private MediatR.IMediator _mediatR = default!;

    private static readonly HO_SpaceRequest SpaceReq = new(42);
    private static readonly HO_MediatorRequest MediatorReq = new(42);
    private static readonly HO_MediatRRequest MediatRReq = new(42);

    [GlobalSetup(Targets = [nameof(Space_Send), nameof(MediatR_Send), nameof(Mediator_Send)])]
    public void Setup()
    {
        // Space
        var spServices = new ServiceCollection();
        spServices.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var spProvider = spServices.BuildServiceProvider();
        _space = spProvider.GetRequiredService<ISpace>();

        // Mediator (martinothamar/Mediator)
        var medServices = new ServiceCollection();
        medServices.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var medProvider = medServices.BuildServiceProvider();
        _mediator = medProvider.GetRequiredService<Mediator.IMediator>();

        // MediatR (jbogard/MediatR)
        var mrServices = new ServiceCollection();
        mrServices.AddMediatR(Assembly.GetExecutingAssembly());
        var mrProvider = mrServices.BuildServiceProvider();
        _mediatR = mrProvider.GetRequiredService<MediatR.IMediator>();

        // Warm-up
        for (int i = 0; i < 10_000; i++)
        {
            _ = _space.Send<HO_SpaceRequest, HO_Response>(SpaceReq).GetAwaiter().GetResult();
            _ = _mediator.Send(MediatorReq).GetAwaiter().GetResult();
            _ = _mediatR.Send(MediatRReq).GetAwaiter().GetResult();
        }
    }

    [Benchmark]
    public ValueTask<HO_Response> Space_Send()
        => _space.Send<HO_SpaceRequest, HO_Response>(SpaceReq);

    [Benchmark]
    public Task<HO_Response> Mediator_Send()
        => _mediator.Send(MediatorReq).AsTask();

    [Benchmark]
    public Task<HO_Response> MediatR_Send()
        => _mediatR.Send(MediatRReq);
}

// Space handler
public sealed class HO_SpaceHandler
{
    [Handle]
    public ValueTask<HO_Response> Handle(HandlerContext<HO_SpaceRequest> ctx)
        => ValueTask.FromResult(new HO_Response(ctx.Request.Id + 1));
}

// Mediator handler
public sealed class HO_MediatorHandler : Mediator.IRequestHandler<HO_MediatorRequest, HO_Response>
{
    public ValueTask<HO_Response> Handle(HO_MediatorRequest request, CancellationToken cancellationToken)
        => ValueTask.FromResult(new HO_Response(request.Id + 1));
}

// MediatR handler
public sealed class HO_MediatRHandler : MediatR.IRequestHandler<HO_MediatRRequest, HO_Response>
{
    public Task<HO_Response> Handle(HO_MediatRRequest request, CancellationToken cancellationToken)
        => Task.FromResult(new HO_Response(request.Id + 1));
}

public readonly record struct HO_SpaceRequest(int Id) : Space.Abstraction.Contracts.IRequest<HO_Response>;
public readonly record struct HO_MediatorRequest(int Id) : Mediator.IRequest<HO_Response>;
public readonly record struct HO_MediatRRequest(int Id) : MediatR.IRequest<HO_Response>;
public readonly record struct HO_Response(int Value);
