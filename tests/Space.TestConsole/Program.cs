using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;
using Space.DependencyInjection;
using Space.TestConsole.Services;

var services = new ServiceCollection();
services.AddScoped<IDataService, DataService>();

Console.WriteLine("Press ENTER to start");
Console.ReadLine();

services.AddSpace(opt =>
{
    opt.ServiceLifetime = ServiceLifetime.Singleton;
    opt.NotificationDispatchType = NotificationDispatchType.Parallel;
});

var sp = services.BuildServiceProvider();
ISpace space = sp.GetRequiredService<ISpace>();

var command = new UserCreateCommand() { Email = "salihcantekin@gmail.com", Name = "SalihCantekin" };


//Console.WriteLine("Press ENTER to start");
//Console.ReadLine();

int counter = 100_000;
//counter = 2;

for (int i = 0; i < counter; i++)
{
    _ = await space.Send<UserCreateResponse>(command);
}

//var res = await space.Send(command);
//res = await space.Send(command);


//Console.ReadKey();

Log.Add("DONE!");



public class TestHandler
//: IHandler<UserCreateCommand, UserCreateResponse>
{
    //[Handle(Name = "NothingHandle")]
    //[CacheModule(Duration = 5)]
    //public ValueTask<Nothing> Handle(HandlerContext<int> ctx)
    //{
    //    var randomNumber = dataService.GetRandomNumber();
    //    Log.Add($"   [TestHandler.NothingHandle] Processing Id: {ctx.Request}, Number: {randomNumber}");
    //    return Nothing.ValueTask;
    //}


    UserCreateResponse res = new("");

    [Handle(IsDefault = true)]
    [AuditModule]
    public ValueTask<UserCreateResponse> Handle(HandlerContext<UserCreateCommand> ctx)
    {
        //var randomNumber = 1; // dataService.GetRandomNumber();
        //Log.Add($"   [TestHandler.UserCreateCommand] Processing Id: {ctx.Request}, Number: {randomNumber}");

        //var result = new UserCreateResponse($"{ctx.Request.Email}_{ctx.Request.Name}_{Random.Shared.Next(1, 100)}");
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