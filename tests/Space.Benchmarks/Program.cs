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
var res = await space.Send<CommandResponse>(new Request(2));

Console.WriteLine("Res: " + res);

[SimpleJob]
[MemoryDiagnoser]
public class Bench
{
    private ISpace space;
    private Mediator.IMediator mediator;
    private MediatR.IMediator mediatR;

    private static readonly Request StaticRequest = new(2);

    [Params(1)] public int N;

    [GlobalSetup(Targets = [nameof(Space_Res), nameof(Space_ReqRes), nameof(Space_Res_Loop), nameof(Space_ReqRes_Loop)])]
    public void SpaceSetup()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        services.AddSpaceSourceGenerated(opt => { opt.ServiceLifetime = ServiceLifetime.Singleton; });
        var sp = services.BuildServiceProvider();
        space = sp.GetRequiredService<ISpace>();

        // Pool warmup: create & return contexts a number of times so steady-state ölçülsün
        for (int i = 0; i < 10_000; i++)
        {
            _ = space.Send<Request, CommandResponse>(StaticRequest).GetAwaiter().GetResult();
        }
    }

    [GlobalSetup(Targets = [nameof(SendMediator), nameof(SendMediator_Loop)])]
    public void MediatorSetup()
    {
        var services = new ServiceCollection();
        services.AddMediator(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        mediator = sp.GetRequiredService<Mediator.IMediator>();
        for (int i = 0; i < 10_000; i++) _ = mediator.Send(new Request(2)).GetAwaiter().GetResult();
    }

    [GlobalSetup]
    public void MediatRSetup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(Assembly.GetExecutingAssembly());
        var sp = services.BuildServiceProvider();
        mediatR = sp.GetRequiredService<MediatR.IMediator>();
    }

    [Benchmark]
    public async Task Space_ReqRes()
    {
        _ = await space.Send<Request, CommandResponse>(new Request(2));
    }

    [Benchmark]
    public async Task Space_Res()
    {
        _ = await space.Send<CommandResponse>(new Request(2));
    }

    [Benchmark]
    public async Task SendMediator()
    {
        _ = await mediator.Send(new Request(2));
    }

    [Benchmark(Description = "Space ReqRes 1K Loop")]
    public async Task Space_ReqRes_Loop()
    {
        for (int i = 0; i < 1_000; i++)
            _ = await space.Send<Request, CommandResponse>(StaticRequest);
    }

    [Benchmark(Description = "Space Res 1K Loop")]
    public async Task Space_Res_Loop()
    {
        for (int i = 0; i < 1_000; i++)
            _ = await space.Send<CommandResponse>(StaticRequest);
    }

    [Benchmark(Description = "Mediator 1K Loop")]
    public async Task SendMediator_Loop()
    {
        for (int i = 0; i < 1_000; i++)
            _ = await mediator.Send(StaticRequest);
    }
}

public class TestHandler
{
    [Handle]
    public ValueTask<CommandResponse> Handle(HandlerContext<Request> request)
    {
        return ValueTask.FromResult(new CommandResponse(2));
    }
}

public record struct Request(int Id) : Mediator.IRequest<CommandResponse> { }
public record struct Request2(int Id) : MediatR.IRequest<CommandResponse> { }
public record struct CommandResponse(int Id);

public class CommandHandler : Mediator.IRequestHandler<Request, CommandResponse>
{
    public ValueTask<CommandResponse> Handle(Request request, CancellationToken cancellationToken) => ValueTask.FromResult(new CommandResponse(2));
}

public class CommandHandler2 : MediatR.IRequestHandler<Request2, CommandResponse>
{
    public Task<CommandResponse> Handle(Request2 request, CancellationToken cancellationToken) => Task.FromResult(new CommandResponse(2));
}
