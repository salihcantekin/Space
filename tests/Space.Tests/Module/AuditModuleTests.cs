using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;
using Space.DependencyInjection;

namespace Space.Tests.Module;

[TestClass]
public class AuditModuleTests
{
    public record AReq(int Id) : IRequest<ARes>;
    public record ARes(string Tag);

    public sealed class AuditHandlers
    {
        [Handle]
        [AuditModule(Profile = "Default", LogLevel = "Info")] // named
        public ValueTask<ARes> NamedProfile(HandlerContext<AReq> ctx)
            => ValueTask.FromResult(new ARes("N"));

        [Handle]
        [AuditModule(Profile = "Dev", LogLevel = "Verbose")] // use named properties
        public ValueTask<ARes> PositionalProfile(HandlerContext<AReq> ctx)
            => ValueTask.FromResult(new ARes("P"));
    }

    // Isolated types for fallback test (no attribute properties set -> use global Default)
    public record BReq(int Id) : IRequest<BRes>;
    public record BRes(string Tag);

    public sealed class AuditHandlersFallback
    {
        [Handle]
        [AuditModule]
        public ValueTask<BRes> Only(HandlerContext<BReq> ctx)
            => ValueTask.FromResult(new BRes("F"));
    }

    // Isolated types for override test (attribute overrides global)
    public record CReq(int Id) : IRequest<CRes>;
    public record CRes(string Tag);

    public sealed class AuditHandlersOverride
    {
        [Handle]
        [AuditModule(LogLevel = "Verbose")]
        public ValueTask<CRes> Only(HandlerContext<CReq> ctx)
            => ValueTask.FromResult(new CRes("O"));
    }

    private static ServiceProvider BuildProvider()
    {
        var services = new ServiceCollection();
        services.AddSpace();
        services.AddSpaceAudit(opt =>
        {
            opt.WithDefaultProfile(o => { o.LogLevel = "Warning"; });
            opt.WithProfile("Dev", o => { o.LogLevel = "Verbose"; });
        });
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Audit_Named_Profile_Should_Be_Resolved()
    {
        using var sp = BuildProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<AuditHandlers>();

        var res = await space.Send<AReq, ARes>(new AReq(1)); // not checking provider behavior, only compilation/registration
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public async Task Audit_Profile_Dev_Should_Be_Resolved()
    {
        using var sp = BuildProvider();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<AuditHandlers>();

        var res = await space.Send<AReq, ARes>(new AReq(2));
        Assert.IsNotNull(res);
    }

    [TestMethod]
    public void Audit_Global_Default_Applies_When_Attribute_Not_Set_Isolated()
    {
        using var sp = BuildProvider();
        _ = sp.GetRequiredService<AuditHandlersFallback>();

        var module = sp.GetRequiredService<AuditModule>();
        var key = ModuleIdentifier.From<BReq, BRes>(module.GetModuleName(), "Default");
        var cfg = (AuditModuleConfig)module.GetModuleConfig(key);
        Assert.AreEqual("Warning", cfg.LogLevel);
    }

    [TestMethod]
    public void Audit_Attribute_Overrides_Global_Default_Isolated()
    {
        using var sp = BuildProvider();
        _ = sp.GetRequiredService<AuditHandlersOverride>();

        var module = sp.GetRequiredService<AuditModule>();
        var key = ModuleIdentifier.From<CReq, CRes>(module.GetModuleName(), "Default");
        var cfg = (AuditModuleConfig)module.GetModuleConfig(key);
        Assert.AreEqual("Verbose", cfg.LogLevel);
    }
}
