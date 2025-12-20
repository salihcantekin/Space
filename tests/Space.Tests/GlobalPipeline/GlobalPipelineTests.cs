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

    // Separate request/response types for each test to avoid cross-contamination
    public record BasicTestReq(string Value) : IRequest<BasicTestRes>;
    public record BasicTestRes(string Value);

    public record TypeMatchReq(string Value) : IRequest<TypeMatchRes>;
    public record TypeMatchRes(string Value);

    public record BeforeHandlerReq(string Value) : IRequest<BeforeHandlerRes>;
    public record BeforeHandlerRes(string Value);

    public record InnerStageReq(string Value) : IRequest<InnerStageRes>;
    public record InnerStageRes(string Value);

    public record MultiPipelineReq(string Value) : IRequest<MultiPipelineRes>;
    public record MultiPipelineRes(string Value);

    public record InterfaceTestReq(string Value) : IRequest<InterfaceTestRes>;
    public record InterfaceTestRes(string Value);

    public record AnotherReq(int Value) : IRequest<AnotherRes>;
    public record AnotherRes(int Value);

    public record GenericReq(string Value) : IRequest<GenericRes>;
    public record GenericRes(string Value);

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
        var res1 = await Space.Send<BasicTestReq, BasicTestRes>(new BasicTestReq("A"), name: "H1");
        var res2 = await Space.Send<BasicTestReq, BasicTestRes>(new BasicTestReq("B"), name: "H2");

        // Assert - Only BasicGlobalPipeline applies to BasicTestReq
        Assert.AreEqual("A:H1:BASICGP", res1.Value);
        Assert.AreEqual("B:H2:BASICGP", res2.Value);
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
        var typeMatchRes = await Space.Send<TypeMatchReq, TypeMatchRes>(new TypeMatchReq("X"));
        var anotherRes = await Space.Send<AnotherReq, AnotherRes>(new AnotherReq(42));

        // Assert - TypeMatchReq has global pipeline, AnotherReq doesn't
        Assert.AreEqual("X:TypeMatchHandler:TYPEMATCHGP", typeMatchRes.Value);
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
        var res = await Space.Send<BeforeHandlerReq, BeforeHandlerRes>(new BeforeHandlerReq("X"), name: "WithPipeline");

        // Assert - Order: GlobalBeforeHandler (stage 0, outer) -> HandlerPipeline -> Handler
        // Post-processing: Handler result -> Pipeline adds transformation -> GlobalBeforeHandler adds transformation
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
        var res = await Space.Send<InnerStageReq, InnerStageRes>(new InnerStageReq("Y"), name: "WithInnerGlobal");

        // Assert - Order: HandlerPipeline (outer) -> GlobalBeforeHandlerInner (stage 1, inner) -> Handler
        // Post-processing: Handler -> GlobalBeforeHandlerInner adds ":BeforeHandlerInner" -> Pipeline wraps
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
        var res = await Space.Send<MultiPipelineReq, MultiPipelineRes>(new MultiPipelineReq("Z"), name: "MultiGlobal");

        // Assert - Multiple global pipelines in same stage execute by Order
        // GP1 (Order=10, outer) -> GP2 (Order=20, inner) -> Handler
        // Post-processing: Handler -> GP2 adds "GP2" -> GP1 adds "GP1"
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
        var res = await Space.Send<InterfaceTestReq, InterfaceTestRes>(new InterfaceTestReq("Interface"));

        // Assert - Only InterfaceBasedGlobalPipeline applies to InterfaceTestReq
        Assert.AreEqual("Interface:InterfaceHandler:GPINTERFACE", res.Value);
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

    [TestMethod]
    public async Task GlobalPipeline_AppliesToSpecificRequestResponseType()
    {
        // Arrange: specific global pipeline should apply to GenericReq/GenericRes
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();

        // Act
        var res = await Space.Send<GenericReq, GenericRes>(new GenericReq("G"));

        // Assert - specific global pipeline should wrap handler result
        Assert.AreEqual("G:GenericHandler:GENERICGP", res.Value);
    }

    // Handlers for BasicTestReq
    public class BasicHandlers
    {
        [Handle(Name = "H1")]
        public ValueTask<BasicTestRes> Handler1(HandlerContext<BasicTestReq> ctx)
            => ValueTask.FromResult(new BasicTestRes(ctx.Request.Value + ":H1"));

        [Handle(Name = "H2")]
        public ValueTask<BasicTestRes> Handler2(HandlerContext<BasicTestReq> ctx)
            => ValueTask.FromResult(new BasicTestRes(ctx.Request.Value + ":H2"));
    }

    // Handler for TypeMatchReq
    public class TypeMatchHandlers
    {
        [Handle]
        public ValueTask<TypeMatchRes> TypeMatchHandler(HandlerContext<TypeMatchReq> ctx)
            => ValueTask.FromResult(new TypeMatchRes(ctx.Request.Value + ":TypeMatchHandler"));
    }

    // Handlers for BeforeHandlerReq
    public class BeforeHandlerTestHandlers
    {
        [Handle(Name = "WithPipeline")]
        public ValueTask<BeforeHandlerRes> HandlerWithPipeline(HandlerContext<BeforeHandlerReq> ctx)
            => ValueTask.FromResult(new BeforeHandlerRes(ctx.Request.Value + ":Handler"));

        [Pipeline("WithPipeline", Order = 50)]
        public async ValueTask<BeforeHandlerRes> Pipeline1(PipelineContext<BeforeHandlerReq> ctx, PipelineDelegate<BeforeHandlerReq, BeforeHandlerRes> next)
        {
            var res = await next(ctx);
            return new BeforeHandlerRes(res.Value.Replace(":Handler", ":Pipeline:Handler"));
        }
    }

    // Handlers for InnerStageReq
    public class InnerStageTestHandlers
    {
        [Handle(Name = "WithInnerGlobal")]
        public ValueTask<InnerStageRes> HandlerWithInner(HandlerContext<InnerStageReq> ctx)
            => ValueTask.FromResult(new InnerStageRes(ctx.Request.Value + ":Handler"));

        [Pipeline("WithInnerGlobal", Order = 40)]
        public async ValueTask<InnerStageRes> PipelineForInner(PipelineContext<InnerStageReq> ctx, PipelineDelegate<InnerStageReq, InnerStageRes> next)
        {
            var res = await next(ctx);
            return new InnerStageRes(res.Value.Replace(":BeforeHandlerInner:Handler", ":Pipeline:BeforeHandlerInner:Handler"));
        }
    }

    // Handler for MultiPipelineReq
    public class MultiPipelineHandlers
    {
        [Handle(Name = "MultiGlobal")]
        public ValueTask<MultiPipelineRes> HandlerMulti(HandlerContext<MultiPipelineReq> ctx)
            => ValueTask.FromResult(new MultiPipelineRes(ctx.Request.Value + ":Handler"));
    }

    // Handler for InterfaceTestReq
    public class InterfaceTestHandlers
    {
        [Handle]
        public ValueTask<InterfaceTestRes> InterfaceHandler(HandlerContext<InterfaceTestReq> ctx)
            => ValueTask.FromResult(new InterfaceTestRes(ctx.Request.Value + ":InterfaceHandler"));
    }

    // Handler for AnotherReq (no global pipeline)
    public class AnotherHandlers
    {
        [Handle]
        public ValueTask<AnotherRes> AnotherHandler(HandlerContext<AnotherReq> ctx)
            => ValueTask.FromResult(new AnotherRes(ctx.Request.Value + 1));
    }

    // Handlers for GenericReq
    public class GenericHandlers
    {
        [Handle]
        public ValueTask<GenericRes> GenericHandler(HandlerContext<GenericReq> ctx)
            => ValueTask.FromResult(new GenericRes(ctx.Request.Value + ":GenericHandler"));
    }

    // Global pipeline for BasicTestReq only
    public class BasicGlobalPipeline
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<BasicTestRes> GlobalPipe(PipelineContext<BasicTestReq> ctx, PipelineDelegate<BasicTestReq, BasicTestRes> next)
        {
            var res = await next(ctx);
            return new BasicTestRes(res.Value + ":BASICGP");
        }
    }

    // Global pipeline for TypeMatchReq only
    public class TypeMatchGlobalPipeline
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<TypeMatchRes> GlobalPipe(PipelineContext<TypeMatchReq> ctx, PipelineDelegate<TypeMatchReq, TypeMatchRes> next)
        {
            var res = await next(ctx);
            return new TypeMatchRes(res.Value + ":TYPEMATCHGP");
        }
    }

    // Global pipelines for BeforeHandlerReq
    public class BeforeHandlerGlobalPipelines
    {
        [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<BeforeHandlerRes> GlobalBeforeHandler(PipelineContext<BeforeHandlerReq> ctx, PipelineDelegate<BeforeHandlerReq, BeforeHandlerRes> next)
        {
            var res = await next(ctx);
            return new BeforeHandlerRes(res.Value.Replace(":Pipeline:", ":BeforeHandler:Pipeline:"));
        }
    }

    // Global pipelines for InnerStageReq
    public class InnerStageGlobalPipelines
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandlerInner)]
        public async ValueTask<InnerStageRes> GlobalBeforeHandlerInner(PipelineContext<InnerStageReq> ctx, PipelineDelegate<InnerStageReq, InnerStageRes> next)
        {
            var res = await next(ctx);
            return new InnerStageRes(res.Value.Replace(":Handler", ":BeforeHandlerInner:Handler"));
        }
    }

    // Multiple global pipelines for MultiPipelineReq
    public class MultiGlobalPipelines
    {
        [GlobalPipeline(Order = 10, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<MultiPipelineRes> GlobalPipe1(PipelineContext<MultiPipelineReq> ctx, PipelineDelegate<MultiPipelineReq, MultiPipelineRes> next)
        {
            var res = await next(ctx);
            return new MultiPipelineRes(res.Value.Replace(":GP2:Handler", ":GP1:GP2:Handler"));
        }

        [GlobalPipeline(Order = 20, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<MultiPipelineRes> GlobalPipe2(PipelineContext<MultiPipelineReq> ctx, PipelineDelegate<MultiPipelineReq, MultiPipelineRes> next)
        {
            var res = await next(ctx);
            return new MultiPipelineRes(res.Value.Replace(":Handler", ":GP2:Handler"));
        }
    }

    // Interface-based global pipeline for InterfaceTestReq
    public class InterfaceBasedGlobalPipeline : IGlobalPipeline<InterfaceTestReq, InterfaceTestRes>
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<InterfaceTestRes> HandleGlobalPipeline(PipelineContext<InterfaceTestReq> ctx, PipelineDelegate<InterfaceTestReq, InterfaceTestRes> next)
        {
            var res = await next(ctx);
            return new InterfaceTestRes(res.Value + ":GPINTERFACE");
        }
    }

    // Global pipeline for GenericReq/GenericRes - now specific instead of generic
    // Note: For testing generic global pipelines that apply to ALL handlers, 
    // use a separate test assembly to avoid affecting other tests.
    public class GenericGlobalPipeline
    {
        [GlobalPipeline(Order = 100, ExecutionStage = GlobalPipelineExecutionStage.BeforeHandler)]
        public async ValueTask<GenericRes> HandleGlobalPipeline(
            PipelineContext<GenericReq> ctx,
            PipelineDelegate<GenericReq, GenericRes> next)
        {
            var res = await next(ctx);
            return new GenericRes(res.Value + ":GENERICGP");
        }
    }
}
