using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Tests.Handle;

[TestClass]
public class HandleTests
{
    public record HandleWithNameRequest(int Id) : IRequest<HandleWithNameResponse>;
    public record HandleWithNameResponse(Guid Id);

    public class TestHandler
    {
        public Func<HandlerContext<int>, ValueTask<int>> HandleIntFunc;
        public Func<HandlerContext<HandleWithNameRequest>, ValueTask<HandleWithNameResponse>> HandleWithNameFunc;
        public Func<HandlerContext<string>, Task<string>> HandleForPipelineFunc;

        [Handle]
        public virtual ValueTask<int> Handle_int_int(HandlerContext<int> ctx)
            => HandleIntFunc != null ? HandleIntFunc(ctx) : ValueTask.FromResult(10);

        [Handle(Name = "This_is_handle_name")]
        public virtual ValueTask<HandleWithNameResponse> Handle_WithName(HandlerContext<HandleWithNameRequest> ctx)
            => HandleWithNameFunc != null ? HandleWithNameFunc(ctx) : ValueTask.FromResult(new HandleWithNameResponse(Guid.NewGuid()));

        [Handle]
        public virtual Task<string> Handle_ForPipeline(HandlerContext<string> ctx)
            => HandleForPipelineFunc != null ? HandleForPipelineFunc(ctx) : Task.FromResult(string.Empty);
    }

    // Additional types for default handler selection tests
    public record DefaultReq(int Id);
    public record DefaultRes(string Tag);

    public class DefaultSelectionHandlers
    {
        [Handle(Name = "A")]
        public ValueTask<DefaultRes> H1(HandlerContext<DefaultReq> ctx)
            => ValueTask.FromResult(new DefaultRes("A"));

        [Handle(Name = "B", IsDefault = true)]
        public ValueTask<DefaultRes> H2(HandlerContext<DefaultReq> ctx)
            => ValueTask.FromResult(new DefaultRes("B-default"));
    }

    private static ServiceProvider sp;
    private static TestHandler handler;
    private static ISpace Space;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        var services = new ServiceCollection();
        services.AddSpace();

        sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        handler = sp.GetRequiredService<TestHandler>();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        sp?.Dispose();
    }

    [TestInitialize]
    public void TestInit()
    {
        handler.HandleIntFunc = null;
        handler.HandleWithNameFunc = null;
        handler.HandleForPipelineFunc = null;
    }

    [TestMethod]
    public async Task Send_WithRequestResponse_ShouldReturnExpected_Func()
    {
        // Arrange
        handler.HandleIntFunc = ctx =>
        {
            Assert.IsNotNull(ctx);
            Assert.AreEqual(5, ctx.Request);
            return ValueTask.FromResult(10);
        };

        // Act
        var res = await Space.Send<int, int>(5);

        // Assert
        Assert.AreEqual(10, res);
    }

    [TestMethod]
    public async Task SendReqRes_WithName_ShouldReturnExpected_Func()
    {
        // Arrange
        handler.HandleWithNameFunc = ctx =>
        {
            Assert.AreEqual(42, ctx.Request.Id);
            return ValueTask.FromResult(new HandleWithNameResponse(Guid.NewGuid()));
        };

        // Act
        var res = await Space.Send<HandleWithNameRequest, HandleWithNameResponse>(new HandleWithNameRequest(42), "This_is_handle_name");

        // Assert
        Assert.IsNotNull(res);
        Assert.IsInstanceOfType(res, typeof(HandleWithNameResponse));
        Assert.IsTrue(res.Id != Guid.Empty);
    }

    [TestMethod]
    public async Task Send_WithName_ShouldReturnExpected_Func()
    {
        // Arrange
        handler.HandleWithNameFunc = ctx =>
        {
            Assert.AreEqual(42, ctx.Request.Id);
            return ValueTask.FromResult(new HandleWithNameResponse(Guid.NewGuid()));
        };

        // Act
        var res = await Space.Send<HandleWithNameResponse>(new HandleWithNameRequest(42), "This_is_handle_name");

        // Assert
        Assert.IsNotNull(res);
        Assert.IsInstanceOfType(res, typeof(HandleWithNameResponse));
        Assert.IsTrue(res.Id != Guid.Empty);
    }

    [TestMethod]
    public async Task Send_ObjectRequest_ShouldReturnExpected_Func()
    {
        // Arrange
        handler.HandleWithNameFunc = ctx =>
        {
            Assert.AreEqual(42, ctx.Request.Id);
            return ValueTask.FromResult(new HandleWithNameResponse(Guid.NewGuid()));
        };

        // Act
        var res = await Space.Send<HandleWithNameResponse>(new HandleWithNameRequest(42), "This_is_handle_name");

        // Assert
        Assert.IsNotNull(res);
        Assert.IsInstanceOfType(res, typeof(HandleWithNameResponse));
        Assert.IsTrue(res.Id != Guid.Empty);
    }

    [TestMethod]
    public async Task Send_IRequest_ShouldReturnExpected_Func()
    {
        // Arrange
        handler.HandleWithNameFunc = ctx =>
        {
            Assert.AreEqual(42, ctx.Request.Id);
            return ValueTask.FromResult(new HandleWithNameResponse(Guid.NewGuid()));
        };

        // Act
        var res = await Space.Send<HandleWithNameResponse>(new HandleWithNameRequest(42), "This_is_handle_name");

        // Assert
        Assert.IsNotNull(res);
        Assert.IsInstanceOfType<HandleWithNameResponse>(res);
        Assert.IsTrue(res.Id != Guid.Empty);
    }

    [TestMethod]
    public async Task Send_WithoutName_Uses_IsDefault_Handler_When_Multiple_Exist()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace();
        var spLocal = services.BuildServiceProvider();
        var spaceLocal = spLocal.GetRequiredService<ISpace>();
        _ = spLocal.GetRequiredService<DefaultSelectionHandlers>();

        // Act
        var res = await spaceLocal.Send<DefaultReq, DefaultRes>(new DefaultReq(1));

        // Assert
        Assert.IsNotNull(res);
        Assert.AreEqual("B-default", res.Tag);
    }

    [TestMethod]
    public async Task Send_WithName_Selects_Specified_Handler_Even_If_Default_Exists()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace();
        var spLocal = services.BuildServiceProvider();
        var spaceLocal = spLocal.GetRequiredService<ISpace>();
        _ = spLocal.GetRequiredService<DefaultSelectionHandlers>();

        // Act
        var resA = await spaceLocal.Send<DefaultReq, DefaultRes>(new DefaultReq(2), name: "A");
        var resB = await spaceLocal.Send<DefaultReq, DefaultRes>(new DefaultReq(3), name: "B");

        // Assert
        Assert.AreEqual("A", resA.Tag);
        Assert.AreEqual("B-default", resB.Tag);
    }
}
