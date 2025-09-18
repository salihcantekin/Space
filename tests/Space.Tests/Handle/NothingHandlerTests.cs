using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;

namespace Space.Tests.Handle;

[TestClass]
public class NothingHandlerTests
{
    public record RefRes(string Tag);
    public record RefReq(int Id) : IRequest<Nothing>;
    public record PlainReq(int Id); // does NOT implement IRequest

    public class NothingHandlers
    {
        [Handle]
        public ValueTask<int> NothingToInt(HandlerContext<Nothing> ctx)
            => ValueTask.FromResult(100);

        [Handle(Name = "N2I")]
        public ValueTask<int> NothingToInt_Named(HandlerContext<Nothing> ctx)
            => ValueTask.FromResult(200);

        [Handle]
        public ValueTask<RefRes> NothingToRefRes(HandlerContext<Nothing> ctx)
            => ValueTask.FromResult(new RefRes("X"));

        [Handle(Name = "N2R")]
        public ValueTask<RefRes> NothingToRefRes_Named(HandlerContext<Nothing> ctx)
            => ValueTask.FromResult(new RefRes("Y"));

        [Handle]
        public ValueTask<Nothing> IntToNothing(HandlerContext<int> ctx)
            => Space.Abstraction.Nothing.ValueTask;

        [Handle(Name = "I2N")]
        public ValueTask<Nothing> IntToNothing_Named(HandlerContext<int> ctx)
            => Space.Abstraction.Nothing.ValueTask;

        [Handle]
        public ValueTask<Nothing> RefReqToNothing(HandlerContext<RefReq> ctx)
            => Space.Abstraction.Nothing.ValueTask;

        [Handle(Name = "R2N")]
        public ValueTask<Nothing> RefReqToNothing_Named(HandlerContext<RefReq> ctx)
            => Space.Abstraction.Nothing.ValueTask;

        [Handle]
        public ValueTask<Nothing> PlainReqToNothing(HandlerContext<PlainReq> ctx)
            => Space.Abstraction.Nothing.ValueTask;

        [Handle(Name = "P2N")]
        public ValueTask<Nothing> PlainReqToNothing_Named(HandlerContext<PlainReq> ctx)
            => Space.Abstraction.Nothing.ValueTask;
    }

    [TestMethod]
    public async Task Nothing_Typed_Send_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing, int>(Nothing.Value);
        Assert.AreEqual(100, res);
    }

    [TestMethod]
    public async Task Nothing_Typed_Send_Named_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing, int>(Nothing.Value, name: "N2I");
        Assert.AreEqual(200, res);
    }

    [TestMethod]
    public async Task Nothing_Typed_Send_To_Record_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing, RefRes>(Nothing.Value);
        Assert.AreEqual("X", res.Tag);
    }

    [TestMethod]
    public async Task Nothing_Typed_Send_To_Record_Named_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing, RefRes>(Nothing.Value, name: "N2R");
        Assert.AreEqual("Y", res.Tag);
    }

    [TestMethod]
    public async Task Struct_Typed_Send_To_Nothing_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<int, Nothing>(5);
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Struct_Typed_Send_To_Nothing_Named_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<int, Nothing>(42, name: "I2N");
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Class_IRequest_Send_To_Nothing_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<RefReq, Nothing>(new RefReq(7));
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Class_IRequest_Send_To_Nothing_Named_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<RefReq, Nothing>(new RefReq(11), name: "R2N");
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Class_Object_Send_To_Nothing_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing>((object)new PlainReq(99));
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Class_Object_Send_To_Nothing_Named_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<NothingHandlers>();

        var res = await space.Send<Nothing>((object)new PlainReq(123), name: "P2N");
        Assert.AreEqual(Nothing.Value, res);
    }
}
