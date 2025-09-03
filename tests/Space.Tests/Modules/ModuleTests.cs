using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;
using Space.DependencyInjection;
using Space.Modules.InMemoryCache;
using Space.Modules.InMemoryCache.Cache;

namespace Space.Tests.Modules;

[TestClass]
//[DoNotParallelize]
public class ModuleTests
{
    public record CreateUser(string Email) : IRequest<UserDto>;
    public record UserDto(string Id);

    public class CreateUserHandler
    {
        public Func<HandlerContext<CreateUser>, ValueTask<UserDto>> HandleFunc;

        [Handle]
        [CacheModule(Duration = 120)]
        [AuditModule]
        public virtual ValueTask<UserDto> Handle(HandlerContext<CreateUser> ctx)
            => HandleFunc != null ? HandleFunc(ctx) : ValueTask.FromResult(new UserDto($"{ctx.Request.Email}:{Guid.NewGuid()}"));
    }

    public class TestAuditProvider : IAuditModuleProvider
    {
        public Func<object, ValueTask> BeforeFunc;
        public Func<object, ValueTask> AfterFunc;

        public ValueTask After<TRequest, TResponse>(TResponse response)
            => AfterFunc != null ? AfterFunc(response) : default;

        public ValueTask Before<TRequest, TResponse>(TRequest request)
            => BeforeFunc != null ? BeforeFunc(request) : default;
    }

    private ISpace space;
    private ServiceProvider sp;
    private CreateUserHandler handler;
    private readonly TestAuditProvider auditProvider = new();


    [TestInitialize]
    public void TestInit()
    {
        //var services = new ServiceCollection();
        //services.AddSpace(opt =>
        //{
        //    opt.NotificationDispatchType = NotificationDispatchType.Sequential;
        //});

        sp = BuildProviderWithModules(auditProvider);

        handler = sp.GetRequiredService<CreateUserHandler>();
        space = sp.GetRequiredService<ISpace>();
    }



    private ServiceProvider BuildProviderWithModules(IAuditModuleProvider auditProvider, bool forceReCreate = false)
    {
        if (sp is not null && forceReCreate is false)
            return sp;

        var services = new ServiceCollection();
        services.AddSpace();

        services.AddSpaceInMemoryCache();

        services.AddSpaceAudit(opt =>
        {
            opt.WithAuditModule(auditProvider);
        });

        return services.BuildServiceProvider();
    }

    [TestMethod]
    public async Task CacheModule_Returns_Cached_Response_Func()
    {
        // Arrange
        int handleCount = 0;

        handler.HandleFunc = ctx =>
        {
            handleCount++;
            return ValueTask.FromResult(new UserDto($"{ctx.Request.Email}:{Guid.NewGuid()}"));
        };

        var req = new CreateUser("a@b.com");

        // Act
        var r1 = await space.Send<CreateUser, UserDto>(req);
        var r2 = await space.Send<CreateUser, UserDto>(req);

        // Assert
        Assert.AreEqual(1, handleCount); // second call should hit cache
        Assert.AreEqual(r1.Id, r2.Id);
    }

    [TestMethod]
    public async Task AuditModule_Before_After_Are_Invoked_Func()
    {
        // Arrange
        int counter = 0;

        auditProvider.BeforeFunc = req => { counter++; return default; };
        auditProvider.AfterFunc = res => { counter++; return default; };


        // Act
        _ = await space.Send<CreateUser, UserDto>(new CreateUser("x@y.com"));

        // Assert
        Assert.AreEqual(2, counter);
    }
}
