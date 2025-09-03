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

//Console.WriteLine("Hello, World!");

BenchmarkRunner.Run<Bench>();
return;

Console.WriteLine("Press ENTER to start");
Console.ReadLine();

Bench b = new();
b.SpaceSetup();
b.MediatRSetup();
b.MediatorSetup();

int counter = 100_000;
counter = 1;

//await b.SendMediator();
for (int i = 0; i < counter; i++)
{
    await b.Space_Res();
    await b.SendMediatR();
    await b.SendMediator();
}

//Console.WriteLine("DONE");


//public static class MediatorExtensions





[SimpleJob]
[MemoryDiagnoser]
public class Bench
{
    private ISpace space;
    private Mediator.IMediator mediator;
    private MediatR.IMediator mediatR;


    [GlobalSetup(Targets = [nameof(Space_Res), nameof(Space_ReqRes)])]
    public void SpaceSetup()
    {
        var services = new ServiceCollection();
        services.AddSpace();

        var sp = services.BuildServiceProvider();
        space = sp.GetRequiredService<ISpace>();
    }


    [GlobalSetup(Targets = [nameof(SendMediator)])]
    public void MediatorSetup()
    {
        var services = new ServiceCollection();
        services.AddMediator(opt =>
        {
            opt.ServiceLifetime = ServiceLifetime.Scoped;
        });

        //services.AddTransient(typeof(Mediator.IPipelineBehavior<Request, CommandResponse>), typeof(CommandHandlerPipeline));

        var sp = services.BuildServiceProvider();
        mediator = sp.GetRequiredService<Mediator.IMediator>();
    }

    [GlobalSetup(Targets = [nameof(SendMediatR)])]
    public void MediatRSetup()
    {
        var services = new ServiceCollection();
        services.AddMediatR(Assembly.GetExecutingAssembly());
        //services.AddTransient(typeof(MediatR.IPipelineBehavior<Request2, CommandResponse>), typeof(CommandHandlerPipeline2));

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

    [Benchmark]
    public async Task SendMediatR()
    {
        _ = await mediatR.Send(new Request2(2));
    }
}


public class TestHandler//: IHandler<Request, CommandResponse>
{
    [Handle]
    public ValueTask<CommandResponse> Handle(HandlerContext<Request> request)
    {
        return ValueTask.FromResult(new CommandResponse(2));
    }

    //[Pipeline]
    //public ValueTask<CommandResponse> Pipeline(PipelineContext<Request> ctx, PipelineDelegate<Request, CommandResponse> next)
    //{
    //    return next(ctx);
    //}



    //public class CommandPipeline : IPipelineBehavior<Request, CommandResponse>
    //{ 
    //    public ValueTask<CommandResponse> Handle(Request message, MessageHandlerDelegate<Request, CommandResponse> next, CancellationToken cancellationToken)
    //    {
    //        return next(message, cancellationToken);
    //    }
    //}
}

public record Request(int Id) : Mediator.IRequest<CommandResponse> { }

public record Request2(int Id) : MediatR.IRequest<CommandResponse> { }

public record CommandResponse(int Id);

public class CommandHandler : Mediator.IRequestHandler<Request, CommandResponse>
{
    public ValueTask<CommandResponse> Handle(Request request, CancellationToken cancellationToken)
    {
        return ValueTask.FromResult(new CommandResponse(2));
    }
}


//public class CommandHandlerPipeline : Mediator.IPipelineBehavior<Request, CommandResponse>
//{
//    public ValueTask<CommandResponse> Handle(Request request, MessageHandlerDelegate<Request, CommandResponse> next, CancellationToken cancellationToken)
//    {
//        return next(request, cancellationToken);
//    }
//}

public class CommandHandler2 : MediatR.IRequestHandler<Request2, CommandResponse>
{
    public Task<CommandResponse> Handle(Request2 request, CancellationToken cancellationToken)
    {
        return Task.FromResult(new CommandResponse(2));
    }
}

//public class CommandHandlerPipeline2 : MediatR.IPipelineBehavior<Request2, CommandResponse>
//{
//    public Task<CommandResponse> Handle(Request2 request, MediatR.RequestHandlerDelegate<CommandResponse> next, CancellationToken cancellationToken)
//    {
//        return next();
//    }
//}
