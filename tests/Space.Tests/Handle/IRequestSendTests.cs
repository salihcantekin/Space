using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Tests.Handle;

[TestClass]
public class IRequestSendTests
{
    public record ReqA(int Id) : IRequest<Res>;
    public record Res(string Value);

    public class DefaultHandler
    {
        [Handle]
        public ValueTask<Res> Handle(HandlerContext<ReqA> ctx)
            => ValueTask.FromResult(new Res($"D:{ctx.Request.Id}"));
    }

    public class NamedHandlers
    {
        [Handle(Name = "A")]
        public ValueTask<Res> H1(HandlerContext<ReqA> ctx)
            => ValueTask.FromResult(new Res($"A:{ctx.Request.Id}"));

        [Handle(Name = "B")]
        public ValueTask<Res> H2(HandlerContext<ReqA> ctx)
            => ValueTask.FromResult(new Res($"B:{ctx.Request.Id}"));
    }

    private ServiceProvider sp;
    private ISpace space;

    [TestInitialize]
    public void Init()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        sp = services.BuildServiceProvider();
        space = sp.GetRequiredService<ISpace>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        sp?.Dispose();
    }

    [TestMethod]
    public async Task IRequest_Overload_Routes_To_Default_Handler()
    {
        IRequest<Res> req = new ReqA(10);
        var res = await space.Send<Res>(req);
        Assert.AreEqual("D:10", res.Value);
    }

    [TestMethod]
    public async Task IRequest_Overload_Selects_Named_Handler()
    {
        IRequest<Res> r1 = new ReqA(1);
        IRequest<Res> r2 = new ReqA(2);

        var a = await space.Send<Res>(r1, name: "A");
        var b = await space.Send<Res>(r2, name: "B");

        Assert.AreEqual("A:1", a.Value);
        Assert.AreEqual("B:2", b.Value);
    }
}
