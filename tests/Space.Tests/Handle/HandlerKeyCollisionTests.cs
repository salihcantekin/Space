using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Tests.Handle;

[TestClass]
public class HandlerKeyCollisionTests
{
    public record Req(int Id) : IRequest<ResA>, IRequest<ResB>;
    public record ResA(string Value);
    public record ResB(string Value);

    // Handler 1: Req -> ResA (no name)
    public class ResAHandler
    {
        [Handle]
        public ValueTask<ResA> Handle(HandlerContext<Req> ctx)
            => ValueTask.FromResult(new ResA($"A:{ctx.Request.Id}"));
    }

    // Handler 2: Req -> ResB (no name)
    public class ResBHandler
    {
        [Handle]
        public ValueTask<ResB> Handle(HandlerContext<Req> ctx)
            => ValueTask.FromResult(new ResB($"B:{ctx.Request.Id}"));
    }

    private static ServiceProvider sp;
    private static ISpace Space;

    [ClassInitialize]
    public static void ClassInit(TestContext context)
    {
        var services = new ServiceCollection();
        services.AddSpace();
        sp = services.BuildServiceProvider();
        Space = sp.GetRequiredService<ISpace>();
    }

    [ClassCleanup]
    public static void ClassCleanup()
    {
        sp?.Dispose();
    }

    [TestMethod]
    public async Task Typed_Send_Disambiguates_By_ResponseType()
    {
        var a = await Space.Send<Req, ResA>(new Req(1));
        var b = await Space.Send<Req, ResB>(new Req(2));

        Assert.IsNotNull(a);
        Assert.IsNotNull(b);
        Assert.AreEqual("A:1", a.Value);
        Assert.AreEqual("B:2", b.Value);
    }

    [TestMethod]
    public async Task Object_Send_Disambiguates_By_ResponseType()
    {
        object req1 = new Req(10);
        object req2 = new Req(20);

        var a = await Space.Send<ResA>(req1);
        var b = await Space.Send<ResB>(req2);

        Assert.IsNotNull(a);
        Assert.IsNotNull(b);
        Assert.AreEqual("A:10", a.Value);
        Assert.AreEqual("B:20", b.Value);
    }
}
