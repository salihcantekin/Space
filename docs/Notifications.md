# Notifications

Space provides a notification system for event-driven communication. Notification handlers are marked with the `[Notification]` attribute and receive events published via the `Publish` method. Notifications can be dispatched in parallel or sequentially.

## Example
```csharp
public record UserLoggedInSuccessfully(string UserName);

public partial class UserHandlers
{
    [Notification]
    public ValueTask LoginNotificationHandlerForFileLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    {
        // log details to file
        return ValueTask.CompletedTask;
    }

    [Notification]
    public ValueTask LoginNotificationHandlerForDbLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    {
        // log details to db
        return ValueTask.CompletedTask;
    }
}

ISpace space = serviceProvider.GetRequiredService<ISpace>();
await space.Publish(new UserLoggedInSuccessfully("sc"));
```

## Configuration
Set the dispatch type during registration:
```csharp
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel; // or Sequential
});
```

## Per-call override
You can override the dispatcher strategy for a single publish call without changing the global configuration:
```csharp
// Force parallel dispatch just for this call
await space.Publish(new UserLoggedInSuccessfully("sc"), NotificationDispatchType.Parallel);

// Or force sequential dispatch for this call
await space.Publish(new UserLoggedInSuccessfully("sc"), NotificationDispatchType.Sequential);
