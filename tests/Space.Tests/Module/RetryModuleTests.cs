using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Retry;
using Space.DependencyInjection;
using System;
using System.Threading.Tasks;

namespace Space.Tests.Module;

[TestClass]
public class RetryModuleTests
{
    public record RetryReq(int FailTimes) : IRequest<RetryRes>;
    public record RetryRes(int Attempts);

    // Handler for Dev profile usage
    public class RetryHandlersDev
    {
        private static int _calls;
        public static void Reset() => _calls = 0;

        [Handle(Name = "RetryDev")] // explicit name to select this handler
        [RetryModule(Profile = "Dev")]
        public ValueTask<RetryRes> DoWork(HandlerContext<RetryReq> ctx)
        {
            if (_calls < ctx.Request.FailTimes)
            {
                _calls++;
                throw new InvalidOperationException("fail");
            }
            _calls++;
            return ValueTask.FromResult(new RetryRes(_calls));
        }
    }

    // Handler for Default profile usage
    public class RetryHandlersDefault
    {
        private static int _calls;
        public static void Reset() => _calls = 0;

        [Handle(Name = "RetryDefault")] // explicit name
        [RetryModule(Profile = "Default")]
        public ValueTask<RetryRes> DoWork(HandlerContext<RetryReq> ctx)
        {
            if (_calls < ctx.Request.FailTimes)
            {
                _calls++;
                throw new InvalidOperationException("fail");
            }
            _calls++;
            return ValueTask.FromResult(new RetryRes(_calls));
        }
    }

    // Handler where attribute overrides global profile settings
    public class RetryHandlersOverride
    {
        private static int _calls;
        public static void Reset() => _calls = 0;

        [Handle(Name = "RetryOverride")]
        [RetryModule(Profile = "Dev", RetryCount = 2, DelayMilliseconds = 0)]
        public ValueTask<RetryRes> DoWork(HandlerContext<RetryReq> ctx)
        {
            if (_calls < ctx.Request.FailTimes)
            {
                _calls++;
                throw new InvalidOperationException("fail");
            }
            _calls++;
            return ValueTask.FromResult(new RetryRes(_calls));
        }
    }

    private static ServiceProvider BuildProvider(Action<RetryModuleOptions> configureRetry)
    {
        var services = new ServiceCollection();
        services.AddSpace();
        services.AddSpaceRetry(configureRetry);
        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task Retry_Profile_Dev_Should_Retry_As_Profile()
    {
        using var sp = BuildProvider(opt =>
        {
            opt.WithProfile("Dev", o => { o.RetryCount = 3; o.DelayMilliseconds = 0; });
        });

        RetryHandlersDev.Reset();
        var space = sp.GetRequiredService<ISpace>();

        // Fail twice, succeed on third attempt -> total calls should be 3
        var res = await space.Send<RetryReq, RetryRes>(new RetryReq(FailTimes: 2), name: "RetryDev");
        Assert.AreEqual(3, res.Attempts);
    }

    [TestMethod]
    public async Task Retry_Profile_Default_Should_Retry_As_Profile()
    {
        using var sp = BuildProvider(opt =>
        {
            opt.WithDefaultProfile(o => { o.RetryCount = 2; o.DelayMilliseconds = 0; });
        });

        RetryHandlersDefault.Reset();
        var space = sp.GetRequiredService<ISpace>();

        var res = await space.Send<RetryReq, RetryRes>(new RetryReq(FailTimes: 1), name: "RetryDefault");
        Assert.AreEqual(2, res.Attempts);
    }

    [TestMethod]
    public async Task Retry_Fails_When_RetryCount_Insufficient()
    {
        // Arrange
        int retryCount = 1;
        int failTimes = 2 + retryCount; // initial call + retries
        using var sp = BuildProvider(opt =>
        {
            opt.WithProfile("Dev", o => { o.RetryCount = retryCount; o.DelayMilliseconds = 0; });
        });

        RetryHandlersDev.Reset();
        var space = sp.GetRequiredService<ISpace>();
        
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await space.Send<RetryReq, RetryRes>(new RetryReq(FailTimes: failTimes), name: "RetryDev");
        });
    }

    [TestMethod]
    public async Task Retry_Attribute_Overrides_Global_Profile_Settings()
    {
        using var sp = BuildProvider(opt =>
        {
            opt.WithProfile("Dev", o => { o.RetryCount = 3; o.DelayMilliseconds = 0; });
        });

        RetryHandlersOverride.Reset();
        var space = sp.GetRequiredService<ISpace>();
        _ = sp.GetRequiredService<RetryHandlersOverride>();

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(async () =>
        {
            await space.Send<RetryReq, RetryRes>(new RetryReq(FailTimes: 3), name: "RetryOverride");
        });
    }
}
