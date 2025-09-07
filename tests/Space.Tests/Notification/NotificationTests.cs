using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;
using System.Diagnostics;
using System.Threading.Tasks;

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
            opt.ServiceLifetime = ServiceLifetime.Singleton;
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
        notificatonHandler.OnPingAFunc = null;
        notificatonHandler.OnPingBFunc = null;
    }

    public class NotificationHandlers
    {
        public Func<NotificationContext<int>, ValueTask> OnIntFunc;
        public Func<NotificationContext<Ping>, ValueTask> OnPingAFunc;
        public Func<NotificationContext<Ping>, ValueTask> OnPingBFunc;

        [Notification]
        public virtual ValueTask OnInt(NotificationContext<int> ctx)
            => OnIntFunc != null ? OnIntFunc(ctx) : ValueTask.CompletedTask;

        [Notification]
        public virtual ValueTask OnPingA(NotificationContext<Ping> ctx)
            => OnPingAFunc != null ? OnPingAFunc(ctx) : ValueTask.CompletedTask;

        [Notification]
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
    public async Task Publish_Dispatches_All_Int_Subscribers_Func()
    {
        // Arrange
        bool called1 = false;
        notificatonHandler.OnIntFunc = _ => { called1 = true; return ValueTask.CompletedTask; };

        // Act
        await Space.Publish(42);

        // Assert
        Assert.IsTrue(called1);
    }

    [TestMethod]
    public async Task Publish_Parallel_Dispatches_All_Type_Subscribers_Func()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt =>
        {
            opt.NotificationDispatchType = NotificationDispatchType.Parallel;
            opt.ServiceLifetime = ServiceLifetime.Singleton;
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

    [TestMethod]
    public async Task Publish_Override_To_Parallel_When_Default_Is_Sequential()
    {
        // Arrange: default Sequential
        var services = new ServiceCollection();
        services.AddSpace(opt =>
        {
            opt.NotificationDispatchType = NotificationDispatchType.Sequential;
            opt.ServiceLifetime = ServiceLifetime.Singleton;
        });

        using var provider = services.BuildServiceProvider();
        var spaceLocal = provider.GetRequiredService<ISpace>();
        var handlers = provider.GetRequiredService<NotificationHandlers>();

        // Short delays for fast test
        const int delayMs = 20;
        handlers.OnPingAFunc = async _ => { await Task.Delay(delayMs); };
        handlers.OnPingBFunc = async _ => { await Task.Delay(delayMs); };

        var sw = Stopwatch.StartNew();

        // Act: override to Parallel
        await spaceLocal.Publish(new Ping(7), NotificationDispatchType.Parallel);

        sw.Stop();

        // Assert: near single-delay rather than sum
        Assert.IsTrue(sw.ElapsedMilliseconds < delayMs * 2, $"Expected parallel < {delayMs * 2}ms, actual {sw.ElapsedMilliseconds}ms");
    }

    [TestMethod]
    public async Task Publish_Override_To_Sequential_When_Default_Is_Parallel()
    {
        // Arrange: default Parallel
        var services = new ServiceCollection();
        services.AddSpace(opt =>
        {
            opt.NotificationDispatchType = NotificationDispatchType.Parallel;
            opt.ServiceLifetime = ServiceLifetime.Singleton;
        });

        using var provider = services.BuildServiceProvider();
        var spaceLocal = provider.GetRequiredService<ISpace>();
        var handlers = provider.GetRequiredService<NotificationHandlers>();

        // Short delays for fast test
        const int delayMs = 20;
        handlers.OnPingAFunc = async _ => { await Task.Delay(delayMs); };
        handlers.OnPingBFunc = async _ => { await Task.Delay(delayMs); };

        var sw = Stopwatch.StartNew();

        // Act: override to Sequential
        await spaceLocal.Publish(new Ping(8), NotificationDispatchType.Sequential);

        sw.Stop();

        // Assert: roughly sum of delays
        Assert.IsTrue(sw.ElapsedMilliseconds >= delayMs * 2, $"Expected sequential >= {delayMs * 2}ms, actual {sw.ElapsedMilliseconds}ms");
    }
}
