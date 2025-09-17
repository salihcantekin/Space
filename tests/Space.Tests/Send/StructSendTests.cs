using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.DependencyInjection;

namespace Space.Tests.Send;

[TestClass]
public class StructSendTests
{
    public class StructHandlers
    {
        [Handle]
        public ValueTask<int> IntToInt(HandlerContext<int> ctx)
            => ValueTask.FromResult(ctx.Request + 1);

        [Handle(Name = "DT")]
        public ValueTask<DateTime> IntToDateTime(HandlerContext<int> ctx)
            => ValueTask.FromResult(new DateTime(2000, 1, 1).AddDays(ctx.Request));
    }

    [TestMethod]
    public async Task Struct_Typed_Send_Unnamed_Works()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<StructHandlers>();

        var res = await space.Send<int, int>(5);
        Assert.AreEqual(6, res);
    }

    [TestMethod]
    public async Task Struct_Typed_Send_Named_Works_With_Different_Response()
    {
        var services = new ServiceCollection();
        services.AddSpace(opt => opt.ServiceLifetime = ServiceLifetime.Singleton);
        var sp = services.BuildServiceProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<StructHandlers>();

        var dt = await space.Send<int, DateTime>(5, name: "DT");
        Assert.AreEqual(new DateTime(2000, 1, 6), dt);
    }
}
