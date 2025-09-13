using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;
using Space.DependencyInjection;
using Space.TestConsole.Services;
using Space.Abstraction.Modules.Retry;

var services = new ServiceCollection();
services.AddScoped<IDataService, DataService>();

Console.WriteLine("Press ENTER to start");
Console.ReadLine();

services.AddSpace(opt =>
{
    opt.ServiceLifetime = ServiceLifetime.Singleton;
    opt.NotificationDispatchType = NotificationDispatchType.Parallel;
});

services.AddSpaceAudit(opt =>
{
    opt.WithModuleProvider(new NullAuditProvider());

    opt.WithProfile("Default", o =>
    {
        o.LogLevel = "Warning";
    });

    opt.WithProfile("Dev", o =>
    {
        o.LogLevel = "Verbose";
    });
});

services.AddSpaceRetry(opt =>
{
    opt.RetryCount = 3;
    opt.DelayMilliseconds = 200;

    opt.WithProfile("Default", o =>
    {
        o.RetryCount = 2;
        o.DelayMilliseconds = 100;
    });

    opt.WithProfile("Dev", o =>
    {
        o.RetryCount = 3;
        o.DelayMilliseconds = 1000;
    });
});

var sp = services.BuildServiceProvider();
ISpace space = sp.GetRequiredService<ISpace>();

var command = new UserCreateCommand() { Email = "salihcantekin@gmail.com", Name = "SalihCantekin" };


//Console.WriteLine("Press ENTER to start");
//Console.ReadLine();

//int counter = 100_000;
////counter = 2;

//for (int i = 0; i < counter; i++)
//{
//    _ = await space.Send<UserCreateResponse>(command);
//}

var res = await space.Send<UserCreateResponse>(command);
//res = await space.Send(command);


//Console.ReadKey();

Log.Add("DONE!");



public class TestHandler
: IHandler<UserCreateCommand, UserCreateResponse>
{
    //[Handle(Name = "NothingHandle")]
    //[CacheModule(Duration = 5)]
    //public ValueTask<Nothing> Handle(HandlerContext<int> ctx)
    //{
    //    var randomNumber = dataService.GetRandomNumber();
    //    Log.Add($"   [TestHandler.NothingHandle] Processing Id: {ctx.Request}, Number: {randomNumber}");
    //    return Nothing.ValueTask;
    //}

    private int counter = 0;

    UserCreateResponse res = new("");

    [Handle(IsDefault = true)]
    //[CacheModule(Duration = 20)]
    [AuditModule(Provider = typeof(NullAuditModuleProvider))]
    //[RetryModule("Dev")]
    //[RetryModule(Profile = "Default")]
    //[AuditModule(Profile = "Default", LogLevel = "Debug")]
    public ValueTask<UserCreateResponse> Handle(HandlerContext<UserCreateCommand> ctx)
    {
        //var randomNumber = 1; // dataService.GetRandomNumber();
        //Log.Add($"   [TestHandler.UserCreateCommand] Processing Id: {ctx.Request}, Number: {randomNumber}");

        //var result = new UserCreateResponse($"{ctx.Request.Email}_{ctx.Request.Name}_{Random.Shared.Next(1, 100)}");

        if (counter < 2)
        {
            counter++;
            Log.Add($"   [TestHandler.UserCreateCommand] Throwing exception for {ctx.Request.Email}_{ctx.Request.Name}");
            throw new Exception("Random exception");
        }

        return ValueTask.FromResult(res);
    }

    //[Handle(Name = "Handle2")]
    //public ValueTask<UserCreateResponse> Handle2(HandlerContext<UserCreateCommand> ctx)
    //{
    //    var randomNumber = dataService.GetRandomNumber();
    //    Log.Add($"   [TestHandler.UserCreateCommand] Processing Id: {ctx.Request}, Number: {randomNumber}");

    //    var result = new UserCreateResponse($"{ctx.Request.Email}_{ctx.Request.Name}_{Random.Shared.Next(1, 100)}");
    //    return ValueTask.FromResult(result);
    //}

    //[Pipeline(Order = 1)]
    //public async ValueTask<UserCreateResponse> HandlePipeline(PipelineContext<UserCreateCommand> ctx, PipelineDelegate<UserCreateCommand, UserCreateResponse> next)
    //{
    //    Log.Add($"   [HandlePipeline1] Processing Id: {ctx.Request} BEFORE");

    //    var val = await next(ctx);

    //    Log.Add($"   [HandlePipeline1] Processing Id: {ctx.Request} AFTER");

    //    return val;
    //}

    //[Pipeline(Order = 2)]
    //public async ValueTask<Nothing> HandlePipeline2(PipelineContext<int> ctx, PipelineDelegate<int, Nothing> next)
    //{
    //    Log.Add($"   [HandlePipeline2] Processing Id: {ctx.Request} BEFORE");

    //    var val = await next(ctx);

    //    Log.Add($"   [HandlePipeline2] Processing Id: {ctx.Request} AFTER");

    //    return val;
    //}
}

//public class OtherTestClass
//{
//    //[Pipeline(Order = -1)]
//    public async ValueTask<Nothing> HandlePipeline(PipelineContext<int> ctx, PipelineDelegate<int, Nothing> next)
//    {
//        Log.Add($"   [OtherTestClass.Invoke] Processing Id: {ctx.Request} BEFORE");

//        var val = await next(ctx);

//        Log.Add($"   [OtherTestClass.Invoke] Processing Id: {ctx.Request} AFTER");

//        return val;
//    }

//    public ValueTask<string> HandlePipeline(PipelineContext<string> ctx, PipelineDelegate<string, string> next)
//    {
//        throw new NotImplementedException();
//    }
//}

public static class Log
{
    public static void Add(string logMessage)
    {
        logMessage = $"[{DateTime.Now:HH:mm:ss.fff}] {logMessage}";
        Console.WriteLine(logMessage);
    }
}

public record UserCreateResponse(string Id);

public record UserCreateCommand : IRequest<UserCreateResponse>
{
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}


public class NullAuditProvider : IAuditModuleProvider
{
    public ValueTask After<TRequest, TResponse>(TResponse response) where TRequest : notnull where TResponse : notnull
    {
        return ValueTask.CompletedTask;
    }
    public ValueTask Before<TRequest, TResponse>(TRequest request) where TRequest : notnull where TResponse : notnull
    {
        return ValueTask.CompletedTask;
    }
}