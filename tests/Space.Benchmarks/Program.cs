// See https://aka.ms/new-console-template for more information
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Mediator;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;
using System.Reflection;

BenchmarkRunner.Run<Bench>();
return;

var services = new ServiceCollection();
services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);

var sp = services.BuildServiceProvider();
var space = sp.GetRequiredService<ISpace>();
var res = await space.Send<CommandResponse>(new SpaceRequest(2));

Console.WriteLine("Res: " + res);

[SimpleJob]
[MemoryDiagnoser]
public class Bench
{
    private ISpace space;
    private Mediator.IMediator mediator;
    private MediatR.IMediator mediatR;

    private static readonly MediatorRequest StaticRequest = new(2);
    private static readonly SpaceRequest StaticSpaceRequest = new(2);

    [Params(1)]
    public int N;

    [GlobalSetup(Targets = [nameof(Space_Res), nameof(Space_ReqRes)])]
    public void SpaceSetup()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        space = sp.GetRequiredService<ISpace>();

        // Pool warmup: create & return contexts a number of times so steady-state ölçülsün
        for (int i = 0; i < 10_000; i++)
        {
            _ = space.Send<SpaceRequest, CommandResponse>(StaticSpaceRequest).GetAwaiter().GetResult();
        }
    }

    [GlobalSetup(Targets = [nameof(SendMediator)])]
    public void MediatorSetup()
    {
        var services = new ServiceCollection();
        services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        mediator = sp.GetRequiredService<Mediator.IMediator>();
        services.AddSingleton(typeof(MediatR.IPipelineBehavior<MediatRRequest, CommandResponse>), typeof(MediatRPipeline));

        for (int i = 0; i < 10_000; i++)
            _ = mediator.Send(new MediatorRequest(2)).GetAwaiter().GetResult();
    }

    [GlobalSetup]
    public void MediatRSetup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(Assembly.GetExecutingAssembly());
        var sp = services.BuildServiceProvider();

        services.AddSingleton(typeof(Mediator.IPipelineBehavior<MediatorRequest, CommandResponse>), typeof(MediatorPipeline));
        mediatR = sp.GetRequiredService<MediatR.IMediator>();
    }

    [Benchmark]
    public async Task Space_ReqRes()
    {
        _ = await space.Send<SpaceRequest, CommandResponse>(new SpaceRequest(2));
    }

    [Benchmark]
    public async Task Space_Res()
    {
        _ = await space.Send<CommandResponse>(new SpaceRequest(2));
    }

    [Benchmark]
    public async Task SendMediator()
    {
        _ = await mediator.Send(new MediatorRequest(2));
    }
}

public class TestHandler
{
    [Handle]
    public ValueTask<CommandResponse> Handle(HandlerContext<SpaceRequest> request)
    {
        return ValueTask.FromResult(new CommandResponse(2));
    }

    [Pipeline]
    public ValueTask<CommandResponse> Pipeline(PipelineContext<SpaceRequest> ctx, PipelineDelegate<SpaceRequest, CommandResponse> next)
    {
        return next(ctx);
    }
}

public record struct SpaceRequest(int Id) : Space.Abstraction.Contracts.IRequest<CommandResponse> { }
public record struct MediatorRequest(int Id) : Mediator.IRequest<CommandResponse> { }
public record struct MediatRRequest(int Id) : MediatR.IRequest<CommandResponse> { }
public record struct CommandResponse(int Id);

public class CommandHandler : Mediator.IRequestHandler<MediatorRequest, CommandResponse>
{
    public ValueTask<CommandResponse> Handle(MediatorRequest request, CancellationToken cancellationToken) => ValueTask.FromResult(new CommandResponse(2));
}

public class MediatorPipeline : Mediator.IPipelineBehavior<MediatorRequest, CommandResponse>
{
    public ValueTask<CommandResponse> Handle(MediatorRequest message, MessageHandlerDelegate<MediatorRequest, CommandResponse> next, CancellationToken cancellationToken)
    {
        return next(message, cancellationToken);
    }
}

public class CommandHandler2 : MediatR.IRequestHandler<MediatRRequest, CommandResponse>
{
    public Task<CommandResponse> Handle(MediatRRequest request, CancellationToken cancellationToken) => Task.FromResult(new CommandResponse(2));
}

public class MediatRPipeline : MediatR.IPipelineBehavior<MediatRRequest, CommandResponse>
{
    public Task<CommandResponse> Handle(MediatRRequest request, RequestHandlerDelegate<CommandResponse> next, CancellationToken cancellationToken)
    {
        return next();
    }
}