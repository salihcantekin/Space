using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;

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

    private PipelineHandler GetHandlerClass()
    {
        var services = new ServiceCollection();
        services.AddSpace();
        var sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
        return sp.GetRequiredService<PipelineHandler>();
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
}
