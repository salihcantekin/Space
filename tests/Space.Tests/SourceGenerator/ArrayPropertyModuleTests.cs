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
        [TestMethod]
        public void Simple_Test_DataMaskingModuleAttribute_Can_Be_Created()
        {
            // Simple test to ensure the attribute can be created with array properties
            var attr = new DataMaskingModuleAttribute
            {
                Profile = "TestProfile",
                AttributeOverridesProfile = false,
                UseJsonRoundtrip = false,
                Rules = new[] { "PhoneNumber|Phone", "Address|Address" }
            };

            Assert.AreEqual("TestProfile", attr.Profile);
            Assert.IsFalse(attr.AttributeOverridesProfile);
            Assert.IsFalse(attr.UseJsonRoundtrip);
            Assert.AreEqual(2, attr.Rules.Length);
            Assert.AreEqual("PhoneNumber|Phone", attr.Rules[0]);
            Assert.AreEqual("Address|Address", attr.Rules[1]);
        }

        [TestMethod] 
        public void DataMaskingModuleAttribute_Default_Values_Work()
        {
            // Test default values work correctly
            var attr = new DataMaskingModuleAttribute();

            Assert.IsNull(attr.Profile);
            Assert.IsTrue(attr.AttributeOverridesProfile);
            Assert.IsTrue(attr.UseJsonRoundtrip);
            Assert.IsNotNull(attr.Rules);
            Assert.AreEqual(0, attr.Rules.Length);
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

        public record TestCommand : IRequest<TestResponse>
        {
            public string Name { get; init; } = string.Empty;
        }

        public record TestResponse(string Message);
    }
}