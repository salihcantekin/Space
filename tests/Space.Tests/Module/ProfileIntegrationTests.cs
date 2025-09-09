using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Space.Abstraction;
using Space.Abstraction.Modules;
using Space.DependencyInjection;
using System.Collections.Generic;

namespace Space.Tests.Module
{
    [TestClass]
    public class ProfileIntegrationTests
    {
        [TestMethod]
        public void ProfileProvider_Registration_Should_Work()
        {
            // Arrange
            var services = new ServiceCollection();

            // Configure SpaceOptions directly and register dependencies manually
            var spaceOptions = new SpaceOptions();
            spaceOptions.ConfigureModuleProfile("TestModule", "Production", config =>
            {
                config["DatabaseTimeout"] = "30";
                config["CacheEnabled"] = "true";
                config["LogLevel"] = "Info";
            });

            services.Configure<SpaceOptions>(opt =>
            {
                opt.ConfigureModuleProfile("TestModule", "Production", config =>
                {
                    config["DatabaseTimeout"] = "30";
                    config["CacheEnabled"] = "true";
                    config["LogLevel"] = "Info";
                });
            });

            services.AddSingleton<IModuleProfileProvider, ModuleProfileProvider>();

            var serviceProvider = services.BuildServiceProvider();

            // Act
            var profileProvider = serviceProvider.GetService<IModuleProfileProvider>();

            // Assert
            Assert.IsNotNull(profileProvider, "IModuleProfileProvider should be registered");

            var prodConfig = profileProvider.GetModuleProfileConfiguration("TestModule", "Production");
            Assert.AreEqual("30", prodConfig["DatabaseTimeout"]);
            Assert.AreEqual("true", prodConfig["CacheEnabled"]);
            Assert.AreEqual("Info", prodConfig["LogLevel"]);
        }

        [TestMethod]
        public void ModuleConfig_With_ProfileProvider_Should_Apply_Defaults()
        {
            // Arrange
            var services = new ServiceCollection();
            
            services.Configure<SpaceOptions>(opt =>
            {
                opt.ConfigureModuleProfile("TestModule", "Production", config =>
                {
                    config["DefaultValue"] = "ProductionDefault";
                    config["SharedValue"] = "FromProfile";
                });
            });

            services.AddSingleton<IModuleProfileProvider, ModuleProfileProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var profileProvider = serviceProvider.GetRequiredService<IModuleProfileProvider>();

            var moduleConfig = new ModuleConfig("TestModule")
            {
                ProfileName = "Production"
            };

            // Simulate what SpaceModule.GetModuleConfig would do
            var profileDefaults = profileProvider.GetModuleProfileConfiguration("TestModule", "Production");
            moduleConfig.SetProfileDefaults(profileDefaults);

            // Set some explicit properties (simulating what source generator would do)
            moduleConfig.SetModuleProperty("ExplicitValue", "SetDirectly");

            // Act & Assert
            Assert.AreEqual("ProductionDefault", moduleConfig.GetModuleProperty("DefaultValue"), "Should get default from profile");
            Assert.AreEqual("FromProfile", moduleConfig.GetModuleProperty("SharedValue"), "Should get shared value from profile");
            Assert.AreEqual("SetDirectly", moduleConfig.GetModuleProperty("ExplicitValue"), "Should get explicit value");

            // Override a profile default
            moduleConfig.SetModuleProperty("DefaultValue", "OverriddenValue");
            Assert.AreEqual("OverriddenValue", moduleConfig.GetModuleProperty("DefaultValue"), "Explicit value should override profile default");
        }

        [TestMethod] 
        public void Different_Profiles_Should_Have_Independent_Configurations()
        {
            // Arrange
            var services = new ServiceCollection();
            
            services.Configure<SpaceOptions>(opt =>
            {
                opt.ConfigureModuleProfile("Cache", "Fast", config =>
                {
                    config["TTL"] = "300"; // 5 minutes
                    config["MaxSize"] = "1000";
                });

                opt.ConfigureModuleProfile("Cache", "Persistent", config =>
                {
                    config["TTL"] = "3600"; // 1 hour 
                    config["MaxSize"] = "10000";
                });
            });

            services.AddSingleton<IModuleProfileProvider, ModuleProfileProvider>();

            var serviceProvider = services.BuildServiceProvider();
            var profileProvider = serviceProvider.GetRequiredService<IModuleProfileProvider>();

            // Act
            var fastConfig = profileProvider.GetModuleProfileConfiguration("Cache", "Fast");
            var persistentConfig = profileProvider.GetModuleProfileConfiguration("Cache", "Persistent");

            // Assert
            Assert.AreEqual("300", fastConfig["TTL"]);
            Assert.AreEqual("1000", fastConfig["MaxSize"]);

            Assert.AreEqual("3600", persistentConfig["TTL"]);
            Assert.AreEqual("10000", persistentConfig["MaxSize"]);

            Assert.AreNotEqual(fastConfig["TTL"], persistentConfig["TTL"], "Profiles should have independent values");
        }
    }
}