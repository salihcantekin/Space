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
            => HandleIntFunc != null ? HandleIntFunc(ctx) : ValueTask.FromResult(0);

        [Handle(Name = "This_is_handle_name")]
        public virtual ValueTask<HandleWithNameResponse> Handle_WithName(HandlerContext<HandleWithNameRequest> ctx)
            => HandleWithNameFunc != null ? HandleWithNameFunc(ctx) : ValueTask.FromResult(new HandleWithNameResponse(Guid.Empty));

        [Handle]
        public virtual Task<string> Handle_ForPipeline(HandlerContext<string> ctx)
            => HandleForPipelineFunc != null ? HandleForPipelineFunc(ctx) : Task.FromResult(string.Empty);
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
}
