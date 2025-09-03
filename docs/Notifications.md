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
    }

    [Notification]
    public ValueTask LoginNotificationHandlerForDbLogging(NotificationContext<UserLoggedInSuccessfully> ctx)
    {
        // log details to db
    }
}

ISpace space = serviceProvider.GetRequiredService<ISpace>();
await space.Publish(new UserLoggedInSuccessfully { UserName = "sc" });
```

## Configuration
Set the dispatch type during registration:
```csharp
services.AddSpace(opt =>
{
    opt.NotificationDispatchType = NotificationDispatchType.Parallel;
});
```
