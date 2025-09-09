using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules;
using Space.DependencyInjection;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Space.Tests.Module
{
    [TestClass]
    public class ProfileConfigurationTests
    {
        [TestMethod]
        public void SpaceOptions_Should_Store_Module_Profile_Configuration()
        {
            // Arrange
            var options = new SpaceOptions();

            // Act
            options.ConfigureModuleProfile("TestModule", "Production", config =>
            {
                config["LogLevel"] = "Info";
                config["MaxSize"] = "1000";
            });

            // Assert
            var config = options.GetModuleProfileConfiguration("TestModule", "Production");
            Assert.AreEqual("Info", config["LogLevel"]);
            Assert.AreEqual("1000", config["MaxSize"]);
        }

        [TestMethod]
        public void SpaceOptions_Should_Return_Empty_For_NonExistent_Profile()
        {
            // Arrange
            var options = new SpaceOptions();

            // Act
            var config = options.GetModuleProfileConfiguration("NonExistent", "Profile");

            // Assert
            Assert.AreEqual(0, config.Count);
        }

        [TestMethod]
        public void ModuleConfig_Should_Fallback_To_Profile_Defaults()
        {
            // Arrange
            var moduleConfig = new ModuleConfig("TestModule")
            {
                ProfileName = "Production"
            };

            var profileDefaults = new Dictionary<string, object>
            {
                ["LogLevel"] = "Info",
                ["MaxSize"] = "1000"
            };

            // Act
            moduleConfig.SetProfileDefaults(profileDefaults);
            moduleConfig.SetModuleProperty("CustomProp", "CustomValue");

            // Assert
            Assert.AreEqual("Info", moduleConfig.GetModuleProperty("LogLevel")); // From profile
            Assert.AreEqual("1000", moduleConfig.GetModuleProperty("MaxSize")); // From profile
            Assert.AreEqual("CustomValue", moduleConfig.GetModuleProperty("CustomProp")); // Explicit
        }

        [TestMethod]
        public void ModuleConfig_Should_Override_Profile_Defaults_With_Explicit_Values()
        {
            // Arrange
            var moduleConfig = new ModuleConfig("TestModule")
            {
                ProfileName = "Production"
            };

            var profileDefaults = new Dictionary<string, object>
            {
                ["LogLevel"] = "Info",
                ["MaxSize"] = "1000"
            };

            // Act
            moduleConfig.SetProfileDefaults(profileDefaults);
            moduleConfig.SetModuleProperty("LogLevel", "Error"); // Override profile default

            // Assert
            Assert.AreEqual("Error", moduleConfig.GetModuleProperty("LogLevel")); // Overridden
            Assert.AreEqual("1000", moduleConfig.GetModuleProperty("MaxSize")); // From profile
        }

        [TestMethod]
        public void ModuleConfig_Should_Return_Null_For_NonExistent_Property()
        {
            // Arrange
            var moduleConfig = new ModuleConfig("TestModule");

            // Act & Assert
            Assert.IsNull(moduleConfig.GetModuleProperty("NonExistent"));
        }

        [TestMethod]
        public void ModuleConfig_Should_Handle_Typed_Properties()
        {
            // Arrange
            var moduleConfig = new ModuleConfig("TestModule");
            var profileDefaults = new Dictionary<string, object>
            {
                ["TimeoutSeconds"] = 30,
                ["IsEnabled"] = true,
                ["MaxCount"] = 100L
            };

            // Act
            moduleConfig.SetProfileDefaults(profileDefaults);

            // Assert
            Assert.AreEqual(30, moduleConfig.GetModuleProperty<int>("TimeoutSeconds"));
            Assert.AreEqual(true, moduleConfig.GetModuleProperty<bool>("IsEnabled"));
            Assert.AreEqual(100L, moduleConfig.GetModuleProperty<long>("MaxCount"));
        }

        // Test records for integration testing
        public record TestCommand : IRequest<TestResponse>
        {
            public string Value { get; init; } = string.Empty;
        }

        public record TestResponse(string Result);
    }
}