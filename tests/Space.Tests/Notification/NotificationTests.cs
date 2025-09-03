using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;

namespace Space.Tests.Notification;

[TestClass]
public class NotificationTests
{
    private static ServiceProvider sp;
    private static NotificationHandlers notificatonHandler;
    private static ISpace Space;

    public record Ping(int Id);

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        var services = new ServiceCollection();
        services.AddSpace(opt =>
        {
            opt.NotificationDispatchType = NotificationDispatchType.Sequential;
        });
        sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        notificatonHandler = sp.GetRequiredService<NotificationHandlers>();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        sp?.Dispose();
    }

    [TestInitialize]
    public void TestInit()
    {
        notificatonHandler.OnIntFunc = null;
        notificatonHandler.OnIntNamedFunc = null;
        notificatonHandler.OnPingAFunc = null;
        notificatonHandler.OnPingBFunc = null;
    }

    public class NotificationHandlers
    {
        public Func<NotificationContext<int>, ValueTask> OnIntFunc;
        public Func<NotificationContext<int>, ValueTask> OnIntNamedFunc;
        public Func<NotificationContext<Ping>, ValueTask> OnPingAFunc;
        public Func<NotificationContext<Ping>, ValueTask> OnPingBFunc;

        [Notification]
        public virtual ValueTask OnInt(NotificationContext<int> ctx)
            => OnIntFunc != null ? OnIntFunc(ctx) : ValueTask.CompletedTask;

        [Notification(HandleName = "named")]
        public virtual ValueTask OnIntNamed(NotificationContext<int> ctx)
            => OnIntNamedFunc != null ? OnIntNamedFunc(ctx) : ValueTask.CompletedTask;

        [Notification(HandleName = "A")]
        public virtual ValueTask OnPingA(NotificationContext<Ping> ctx)
            => OnPingAFunc != null ? OnPingAFunc(ctx) : ValueTask.CompletedTask;

        [Notification(HandleName = "B")]
        public virtual ValueTask OnPingB(NotificationContext<Ping> ctx)
            => OnPingBFunc != null ? OnPingBFunc(ctx) : ValueTask.CompletedTask;
    }

    [TestMethod]
    public async Task Publish_ToAllSubscribers_ByType_Func()
    {
        // Arrange
        bool called = false;
        NotificationContext<int>? receivedCtx = null;
        notificatonHandler.OnIntFunc = ctx =>
        {
            called = true;
            receivedCtx = ctx;
            return ValueTask.CompletedTask;
        };

        // Act
        await Space.Publish(5);

        // Assert
        Assert.IsTrue(called);
        Assert.IsNotNull(receivedCtx);
        Assert.AreEqual(5, receivedCtx.Request);
    }

    [TestMethod]
    public async Task Publish_WithName_TargetsOnlyMatching_Func()
    {
        // Arrange
        bool namedCalled = false;
        NotificationContext<int>? receivedCtx = null;
        notificatonHandler.OnIntNamedFunc = ctx =>
        {
            namedCalled = true;
            receivedCtx = ctx;
            return ValueTask.CompletedTask;
        };

        // Act
        await Space.Publish(5, name: "named");

        // Assert
        Assert.IsTrue(namedCalled);
        Assert.IsNotNull(receivedCtx);
        Assert.AreEqual(5, receivedCtx.Request);
    }

    [TestMethod]
    public async Task Publish_Parallel_Dispatches_All_Type_Subscribers_Func()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt =>
        {
            opt.NotificationDispatchType = NotificationDispatchType.Parallel;
        });
        using var provider = services.BuildServiceProvider();
        var SpaceParallel = provider.GetRequiredService<ISpace>();
        var parallelHandlers = provider.GetRequiredService<NotificationHandlers>();

        bool pingACalled = false, pingBCalled = false;
        NotificationContext<Ping> ctxA = null, ctxB = null;

        parallelHandlers.OnPingAFunc = ctx =>
        {
            pingACalled = true;
            ctxA = ctx;
            return ValueTask.CompletedTask;
        };

        parallelHandlers.OnPingBFunc = ctx =>
        {
            pingBCalled = true;
            ctxB = ctx;
            return ValueTask.CompletedTask;
        };

        // Act
        await SpaceParallel.Publish(new Ping(1));

        // Assert
        Assert.IsTrue(pingACalled);
        Assert.IsTrue(pingBCalled);
        Assert.IsNotNull(ctxA);
        Assert.IsNotNull(ctxB);
        Assert.AreEqual(1, ctxA.Request.Id);
        Assert.AreEqual(1, ctxB.Request.Id);
    }
}
