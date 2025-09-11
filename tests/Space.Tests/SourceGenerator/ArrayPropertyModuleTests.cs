using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules;
using Space.Abstraction.Modules.Contracts;
using Space.DependencyInjection;
using System;

namespace Space.Tests.SourceGenerator
{
    [TestClass]
    public class ArrayPropertyModuleTests
    {
        private static ServiceProvider sp = default!;
        private static ISpace space = default!;

        [ClassInitialize]
        public static void ClassInit(TestContext context)
        {
            var services = new ServiceCollection();

            services.AddSpace(opt =>
            {
                opt.ServiceLifetime = ServiceLifetime.Singleton;
            });

            sp = services.BuildServiceProvider();
            space = sp.GetRequiredService<ISpace>();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            sp?.Dispose();
        }

        [TestMethod]
        public async Task DataMaskingModule_Properties_Should_Include_Array_Property()
        {
            var cmd = new TestCommand { Name = "Test" };

            // This should work if array properties are handled correctly
            var result = await space.Send<TestResponse>(cmd, name: "DataMaskingHandler");

            Assert.IsNotNull(result);
            Assert.AreEqual("DataMasked: Test", result.Message);
        }

        /// <summary>
        /// Test attribute that reproduces the issue with array properties
        /// </summary>
        [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
        public sealed class DataMaskingModuleAttribute : Attribute, ISpaceModuleAttribute
        {
            public string? Profile { get; set; }
            public bool AttributeOverridesProfile { get; set; } = true;
            public bool UseJsonRoundtrip { get; set; } = true;
            public string[] Rules { get; set; } = Array.Empty<string>();
        }

        public sealed class TestHandlers
        {
            [Handle(Name = "DataMaskingHandler")]
            [DataMaskingModule(
                Profile = null,
                AttributeOverridesProfile = true,
                UseJsonRoundtrip = true,
                Rules = new[] { "PhoneNumber|Phone", "Address|Address" })]
            public ValueTask<TestResponse> HandleWithArrayProperty(HandlerContext<TestCommand> ctx)
            {
                return ValueTask.FromResult(new TestResponse("DataMasked: " + ctx.Request.Name));
            }

            [Handle(Name = "WithoutArrayHandler")]
            [DataMaskingModule(
                Profile = "DefaultProfile",
                AttributeOverridesProfile = false,
                UseJsonRoundtrip = false)]
            public ValueTask<TestResponse> HandleWithoutArrayProperty(HandlerContext<TestCommand> ctx)
            {
                return ValueTask.FromResult(new TestResponse("Without Array: " + ctx.Request.Name));
            }
        }

        public record TestCommand : IRequest<TestResponse>
        {
            public string Name { get; init; } = string.Empty;
        }

        public record TestResponse(string Message);
    }
}