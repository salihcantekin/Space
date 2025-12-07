using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Tests.GlobalPipeline;

[TestClass]
public class GlobalPipelineTests
{
    private ISpace Space;

    public record TestReq(string Value) : IRequest<TestRes>;
    public record TestRes(string Value);

    public record AnotherReq(int Value) : IRequest<AnotherRes>;
    public record AnotherRes(int Value);

    [TestCleanup]
    public void Cleanup()
    {
        Space = null;
    }

    [TestMethod]
    public async Task GlobalPipeline_ExecutesForAllHandlers_WhenNoExecutionStageSpecified()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res1 = await Space.Send<TestReq, TestRes>(new TestReq("A"), name: "H1");
        var res2 = await Space.Send<TestReq, TestRes>(new TestReq("B"), name: "H2");

        // Assert
        Assert.AreEqual("A:H1:GP", res1.Value);
        Assert.AreEqual("B:H2:GP", res2.Value);
    }

    [TestMethod]
    public async Task GlobalPipeline_OnlyAffectsMatchingRequestResponse()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var testRes = await Space.Send<TestReq, TestRes>(new TestReq("X"));
        var anotherRes = await Space.Send<AnotherReq, AnotherRes>(new AnotherReq(42));

        // Assert - TestReq has global pipeline, AnotherReq doesn't
        Assert.AreEqual("X:TestHandler:GP", testRes.Value);
        Assert.AreEqual(43, anotherRes.Value); // No global pipeline
    }

    [TestMethod]
    public async Task GlobalPipeline_ExecutionStage_BeforeHandler_ExecutesFirst()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<TestReq, TestRes>(new TestReq("X"), name: "WithPipeline");

        // Assert - Order: GlobalBeforeHandler -> HandlerPipeline -> Handler
        Assert.AreEqual("X:BeforeHandler:Pipeline:Handler", res.Value);
    }

    [TestMethod]
    public async Task GlobalPipeline_ExecutionStage_BeforeHandlerInner_ExecutesAfterHandlerPipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<TestReq, TestRes>(new TestReq("Y"), name: "WithInnerGlobal");

        // Assert - Order: HandlerPipeline -> GlobalBeforeHandlerInner -> Handler
        Assert.AreEqual("Y:Pipeline:BeforeHandlerInner:Handler", res.Value);
    }

    [TestMethod]
    public async Task GlobalPipeline_MultipleGlobalPipelines_ExecuteInOrderWithinStage()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<TestReq, TestRes>(new TestReq("Z"), name: "MultiGlobal");

        // Assert - GlobalPipeline1 (Order=10) -> GlobalPipeline2 (Order=20) -> Handler
        Assert.AreEqual("Z:GP1:GP2:Handler", res.Value);
    }

    [TestMethod]
    public async Task GlobalPipeline_WithIGlobalPipelineInterface_CompilesAndExecutes()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<TestReq, TestRes>(new TestReq("Interface"));

        // Assert
        Assert.AreEqual("Interface:TestHandler:GP", res.Value);
    }

    [TestMethod]
    public async Task NoGlobalPipeline_PerformanceUnaffected()
    {
        // Arrange - handler without global pipelines
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<AnotherReq, AnotherRes>(new AnotherReq(100));

        // Assert - Simple handler, no pipeline overhead
        Assert.AreEqual(101, res.Value);
    }

    // Test handlers and pipelines
    public class TestHandlers
    {
        [Handle(Name = "H1")]
        public ValueTask<TestRes> Handler1(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":H1"));

        [Handle(Name = "H2")]
        public ValueTask<TestRes> Handler2(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":H2"));

        [Handle]
        public ValueTask<TestRes> TestHandler(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":TestHandler"));

        [Handle(Name = "WithPipeline")]
        public ValueTask<TestRes> HandlerWithPipeline(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":Handler"));

        [Pipeline("WithPipeline", Order = 50)]
        public async ValueTask<TestRes> Pipeline1(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":Handler", ":Pipeline:Handler"));
        }

        [Handle(Name = "WithInnerGlobal")]
        public ValueTask<TestRes> HandlerWithInner(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":Handler"));

        [Pipeline("WithInnerGlobal", Order = 40)]
        public async ValueTask<TestRes> PipelineForInner(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":BeforeHandlerInner:Handler", ":Pipeline:BeforeHandlerInner:Handler"));
        }

        [Handle(Name = "MultiGlobal")]
        public ValueTask<TestRes> HandlerMulti(HandlerContext<TestReq> ctx)
            => ValueTask.FromResult(new TestRes(ctx.Request.Value + ":Handler"));

        [Handle]
        public ValueTask<AnotherRes> AnotherHandler(HandlerContext<AnotherReq> ctx)
            => ValueTask.FromResult(new AnotherRes(ctx.Request.Value + 1));
    }

    public class GlobalTestPipeline
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TestRes> GlobalPipe(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value + ":GP");
        }

        [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TestRes> GlobalBeforeHandler(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":Pipeline:", ":BeforeHandler:Pipeline:"));
        }

        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandlerInner)]
        public async ValueTask<TestRes> GlobalBeforeHandlerInner(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":Handler", ":BeforeHandlerInner:Handler"));
        }

        [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TestRes> GlobalPipe1(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":Handler", ":GP1:GP2:Handler").Replace(":GP2:GP2:", ":GP2:"));
        }

        [GlobalPipeline(Order = 20, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TestRes> GlobalPipe2(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value.Replace(":GP1:", ":GP1:GP2:"));
        }
    }

    // Test with IGlobalPipeline interface
    public class InterfaceBasedGlobalPipeline : IGlobalPipeline<TestReq, TestRes>
    {
        [GlobalPipeline]
        public async ValueTask<TestRes> HandleGlobalPipeline(PipelineContext<TestReq> ctx, PipelineDelegate<TestReq, TestRes> next)
        {
            var res = await next(ctx);
            return new TestRes(res.Value + ":GP");
        }
    }
}
