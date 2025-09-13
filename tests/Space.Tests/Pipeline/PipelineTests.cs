using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;
using System.Reflection;

namespace Space.Tests.Pipeline;

[TestClass]
public class PipelineTests
{
    private ISpace Space;
    public record Req(string Text);
    public record Res(string Text);

    public class PipelineHandler
    {
        public Func<HandlerContext<Req>, ValueTask<Res>> HandleFunc;
        public Func<PipelineContext<Req>, PipelineDelegate<Req, Res>, ValueTask<Res>> P2Func;
        public Func<PipelineContext<Req>, PipelineDelegate<Req, Res>, ValueTask<Res>> P1Func;

        [Handle(Name = "hn")]
        public virtual ValueTask<Res> Handle(HandlerContext<Req> ctx)
            => HandleFunc != null ? HandleFunc(ctx) : ValueTask.FromResult(new Res(ctx.Request.Text + ":H"));

        [Pipeline("hn", Order = 2)]
        public virtual ValueTask<Res> P2(PipelineContext<Req> ctx, PipelineDelegate<Req, Res> next)
            => P2Func != null ? P2Func(ctx, next) : next(ctx);

        [Pipeline("hn", Order = 1)]
        public virtual ValueTask<Res> P1(PipelineContext<Req> ctx, PipelineDelegate<Req, Res> next)
            => P1Func != null ? P1Func(ctx, next) : next(ctx);
    }

    // New class with two named handlers (same Req/Res) and global pipelines (no handler name)
    public class MultiNamedHandlersWithGlobalPipelines
    {
        public Func<HandlerContext<Req>, ValueTask<Res>> H1Func;
        public Func<HandlerContext<Req>, ValueTask<Res>> H2Func;
        public Func<PipelineContext<Req>, PipelineDelegate<Req, Res>, ValueTask<Res>> P1Func;
        public Func<PipelineContext<Req>, PipelineDelegate<Req, Res>, ValueTask<Res>> P2Func;

        [Handle(Name = "A")]
        public virtual ValueTask<Res> H1(HandlerContext<Req> ctx)
            => H1Func != null ? H1Func(ctx) : ValueTask.FromResult(new Res(ctx.Request.Text + ":H1"));

        [Handle(Name = "B")]
        public virtual ValueTask<Res> H2(HandlerContext<Req> ctx)
            => H2Func != null ? H2Func(ctx) : ValueTask.FromResult(new Res(ctx.Request.Text + ":H2"));

        [Pipeline(Order = 2)]
        public virtual ValueTask<Res> P2Global(PipelineContext<Req> ctx, PipelineDelegate<Req, Res> next)
            => P2Func != null ? P2Func(ctx, next) : next(ctx);

        [Pipeline(Order = 1)]
        public virtual ValueTask<Res> P1Global(PipelineContext<Req> ctx, PipelineDelegate<Req, Res> next)
            => P1Func != null ? P1Func(ctx, next) : next(ctx);
    }

    private PipelineHandler GetHandlerClass()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        return sp.GetRequiredService<PipelineHandler>();
    }

    [TestCleanup]
    public void TestCleanup()
    {
        // Clear Space.EntryCache<Req,Res> to avoid leaking cached entries between tests
        ClearSpaceEntryCache<Req, Res>();
    }

    private static void ClearSpaceEntryCache<TReq, TRes>()
    {
        var spaceType = typeof(Space.DependencyInjection.Space);
        var generic = spaceType.GetNestedType("EntryCache`2", BindingFlags.NonPublic | BindingFlags.Static);
        if (generic == null) return;
        var closed = generic.MakeGenericType(typeof(TReq), typeof(TRes));

        foreach (var f in closed.GetFields(BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Public))
        {
            if (f.FieldType.IsValueType)
            {
                if (f.FieldType == typeof(bool)) f.SetValue(null, false);
                else if (f.FieldType.IsPrimitive) f.SetValue(null, Activator.CreateInstance(f.FieldType));
            }
            else
            {
                f.SetValue(null, null);
            }
        }
    }

    [TestMethod]
    public async Task Pipeline_Order_Is_Applied_And_Next_Called_Func()
    {
        // Arrange
        var handler = GetHandlerClass();
        handler.HandleFunc = ctx => ValueTask.FromResult(new Res(ctx.Request.Text + ":H"));
        handler.P2Func = async (ctx, next) =>
        {
            var res = await next(ctx);
            return res with { Text = res.Text + ":P2" };
        };
        handler.P1Func = async (ctx, next) =>
        {
            var res = await next(ctx);
            return res with { Text = res.Text + ":P1" };
        };

        // Act
        var req = new Req("X");
        var res = await Space.Send<Req, Res>(req, name: "hn");

        // Assert
        Assert.AreEqual("X:H:P2:P1", res.Text);
    }

    [TestMethod]
    public async Task Unnamed_Send_Uses_Last_Handler_And_Global_Pipelines_Execute()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        var handler = sp.GetRequiredService<MultiNamedHandlersWithGlobalPipelines>();

        handler.H1Func = ctx => ValueTask.FromResult(new Res(ctx.Request.Text + ":H1"));
        handler.H2Func = ctx => ValueTask.FromResult(new Res(ctx.Request.Text + ":H2"));

        handler.P2Func = async (ctx, next) =>
        {
            var res = await next(ctx);
            return res with { Text = res.Text + ":P2" };
        };

        handler.P1Func = async (ctx, next) =>
        {
            var res = await next(ctx);
            return res with { Text = res.Text + ":P1" };
        };

        // Act + Assert
        // No name provided -> should use the last registered handler (H2) and run both global pipelines
        var resDefault = await Space.Send<Req, Res>(new Req("X"));
        Assert.AreEqual("X:H2:P2:P1", resDefault.Text);

        // Explicit names still run the same global pipelines
        var resA = await Space.Send<Req, Res>(new Req("A"), name: "A");
        Assert.AreEqual("A:H1:P2:P1", resA.Text);

        var resB = await Space.Send<Req, Res>(new Req("B"), name: "B");
        Assert.AreEqual("B:H2:P2:P1", resB.Text);
    }

    [TestMethod]
    public async Task PipelineContext_Items_Are_Shared_Between_Pipelines()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        var handler = sp.GetRequiredService<MultiNamedHandlersWithGlobalPipelines>();

        handler.H1Func = ctx => ValueTask.FromResult(new Res(ctx.Request.Text + ":H1"));
        handler.H2Func = ctx => ValueTask.FromResult(new Res(ctx.Request.Text + ":H2"));

        handler.P1Func = async (ctx, next) =>
        {
            ctx.SetItem("k", "v");
            var res = await next(ctx);
            var v1 = (string)ctx.GetItem("k");
            return res with { Text = res.Text + $":P1={v1}" };
        };

        handler.P2Func = async (ctx, next) =>
        {
            var v2 = (string)ctx.GetItem("k");
            var res = await next(ctx);
            return res with { Text = res.Text + $":P2={v2}" };
        };

        // Act
        var resDefault = await Space.Send<Req, Res>(new Req("X"));

        // Assert
        Assert.AreEqual("X:H2:P2=v:P1=v", resDefault.Text);
    }
}
