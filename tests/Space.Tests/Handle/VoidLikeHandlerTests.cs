using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.DependencyInjection;
using System.Threading.Tasks;

namespace Space.Tests.Handle;

[TestClass]
public class VoidLikeHandlerTests
{
    public record RefReqVT(int Id) : IRequest<Nothing>;
    public record RefReqT(int Id) : IRequest<Nothing>;
    public record PlainReq(int Id);
    public readonly struct PlainStructReq(int Id)
    {
        public int Id { get; } = Id;
    }

    public class VoidLikeHandlers
    {
        [Handle]
        public ValueTask Handle_VT(HandlerContext<RefReqVT> ctx)
        {
            // simulate async path
            if (ctx.Request.Id == -1)
                return new ValueTask(Task.Delay(1));

            return ValueTask.CompletedTask;
        }

        [Handle(Name = "NamedVT")]
        public ValueTask Handle_VT_Named(HandlerContext<RefReqVT> ctx)
            => ValueTask.CompletedTask;

        [Handle]
        public Task Handle_T(HandlerContext<RefReqT> ctx)
        {
            return Task.CompletedTask;
        }

        [Handle(Name = "NamedT")]
        public Task Handle_T_Named(HandlerContext<RefReqT> ctx)
            => Task.CompletedTask;

        [Handle]
        public ValueTask Handle_Plain(HandlerContext<PlainReq> ctx)
            => ValueTask.CompletedTask;

        [Handle]
        public Task Handle_PlainStruct(HandlerContext<PlainStructReq> ctx)
            => Task.CompletedTask;
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
        _ = sp.GetRequiredService<VoidLikeHandlers>();
    }

    [TestCleanup]
    public void Cleanup()
    {
        sp?.Dispose();
    }

    [TestMethod]
    public async Task IRequest_Nothing_VT_Unnamed_Works()
    {
        var res = await space.Send<RefReqVT, Nothing>(new RefReqVT(1));
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task IRequest_Nothing_VT_Named_Works()
    {
        var res = await space.Send<RefReqVT, Nothing>(new RefReqVT(2), name: "NamedVT");
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task IRequest_Nothing_T_Unnamed_Works()
    {
        var res = await space.Send<RefReqT, Nothing>(new RefReqT(3));
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task IRequest_Nothing_T_Named_Works()
    {
        var res = await space.Send<RefReqT, Nothing>(new RefReqT(4), name: "NamedT");
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Plain_Class_Object_Send_To_Nothing_Works()
    {
        var res = await space.Send<Nothing>((object)new PlainReq(10));
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task Plain_Struct_Typed_Send_To_Nothing_Works()
    {
        var s = new PlainStructReq(11);
        var res = await space.Send<PlainStructReq, Nothing>(in s);
        Assert.AreEqual(Nothing.Value, res);
    }

    [TestMethod]
    public async Task NonGeneric_Send_Object_Works()
    {
        await space.Send((object)new PlainReq(12));
    }
}
