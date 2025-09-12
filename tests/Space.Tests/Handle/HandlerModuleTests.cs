using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;
using Space.DependencyInjection;

namespace Space.Tests.Handle
{
    [TestClass]
    public class HandlerModuleTests
    {
        private static ServiceProvider sp = default!;
        private static ISpace space = default!;
        private static TestAudit audit = default!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var services = new ServiceCollection();

            audit = new TestAudit();

            services.AddSpace(opt =>
            {
                opt.ServiceLifetime = ServiceLifetime.Singleton;
                opt.NotificationDispatchType = NotificationDispatchType.Parallel;
            });

            services.AddSpaceAudit(opt => opt.WithModuleProvider(audit));

            sp = services.BuildServiceProvider();
            space = sp.GetRequiredService<ISpace>();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            sp?.Dispose();
        }

        [TestMethod]
        public async Task Audit_Module_Runs_Only_On_Annotated_Named_Handler()
        {
            var cmd = new UserCreateCommand { Email = "e@example.com", Name = "n" };

            // Annotated handler (Handle1) — should invoke audit
            audit.Reset();

            await space.Send<UserCreateResponse>(cmd, name: "Handle1");

            Assert.AreEqual(1, audit.BeforeCalls, "Audit 'Before' should run once for Handle1.");
            Assert.AreEqual(1, audit.AfterCalls, "Audit 'After' should run once for Handle1.");

            // Non-annotated handler (Handle2) — must NOT invoke audit
            audit.Reset();

            await space.Send<UserCreateResponse>(cmd, name: "Handle2");

            Assert.AreEqual(0, audit.BeforeCalls, "Audit 'Before' must not run for Handle2.");
            Assert.AreEqual(0, audit.AfterCalls, "Audit 'After' must not run for Handle2.");
        }

        private sealed class TestAudit : IAuditModuleProvider
        {
            public int BeforeCalls { get; private set; }
            public int AfterCalls { get; private set; }

            public ValueTask Before<TRequest, TResponse>(TRequest request)
            {
                BeforeCalls++;
                return ValueTask.CompletedTask;
            }

            public ValueTask After<TRequest, TResponse>(TResponse response)
            {
                AfterCalls++;
                return ValueTask.CompletedTask;
            }

            public void Reset()
            {
                BeforeCalls = 0;
                AfterCalls = 0;
            }

            public void SetGlobalOptions<T>(T value)
            {
                
            }
        }

        public sealed class TestHandlers
        {
            private static readonly UserCreateResponse _res = new("");

            [Handle(Name = "Handle1")]
            [AuditModule]
            public ValueTask<UserCreateResponse> Handle1(HandlerContext<UserCreateCommand> ctx)
                => ValueTask.FromResult(_res);

            [Handle(Name = "Handle2")]
            public ValueTask<UserCreateResponse> Handle2(HandlerContext<UserCreateCommand> ctx)
                => ValueTask.FromResult(_res);
        }

        public record UserCreateCommand : IRequest<UserCreateResponse>
        {
            public string Name { get; init; } = string.Empty;
            public string Email { get; init; } = string.Empty;
        }

        public record UserCreateResponse(string Id);
    }
}
