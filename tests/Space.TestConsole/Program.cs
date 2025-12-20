using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Modules.Audit;
using Space.Abstraction.Modules.Retry;
using Space.DependencyInjection;
using Space.TestConsole;
using Space.TestConsole.Services;

var services = new ServiceCollection();
services.AddScoped<IDataService, DataService>();

//Console.WriteLine("Press ENTER to start");
//Console.ReadLine();

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

Log.Add("=== Testing Global Pipeline with Valid Request ===");
var validResult = await space.Send<UserCreateCommand, UserCreateResponse>(command);
Log.Add($"Result: {validResult.Id}");

Log.Add("\n=== Testing Global Pipeline with Invalid Request (Empty Email) ===");
try
{
    var invalidCommand = new UserCreateCommand() { Email = "", Name = "Test" };
    await space.Send<UserCreateCommand, UserCreateResponse>(invalidCommand);
}
catch (ValidationException ex)
{
    Log.Add($"Validation Error: {ex.Message}");
}

Log.Add("\n=== Testing Global Pipeline with int (No Validation) ===");
await space.Send(5);

Log.Add("\nDONE!");

Console.ReadLine();