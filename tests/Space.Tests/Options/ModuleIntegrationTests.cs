using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Space.Abstraction;
using Space.Abstraction.Modules;
using Space.Abstraction.Modules.Audit;
using Space.Abstraction.Modules.Retry;
using System;

namespace Space.Tests.Options;

// Test types for the tests
public record TestRequest(string Data);
public record TestResponse(string Message);

[TestClass]
public class ModuleIntegrationTests
{
    [TestMethod]
    public void AuditModule_ShouldUseOptionsPattern_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceAuditOptions(options =>
        {
            options.LogLevel = "Debug";
            options.IncludeRequestDetails = false;
        });
        services.AddSingleton<AuditModule>();

        var serviceProvider = services.BuildServiceProvider();
        var auditModule = serviceProvider.GetRequiredService<AuditModule>();
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var config = auditModule.GetModuleConfig(moduleIdentifier);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsInstanceOfType(config, typeof(AuditModuleConfig));
        var auditConfig = (AuditModuleConfig)config;
        Assert.AreEqual("Debug", auditConfig.LogLevel);
    }

    [TestMethod]
    public void RetryModule_ShouldUseOptionsPattern_WhenRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceRetryOptions(options =>
        {
            options.RetryCount = 7;
            options.DelayMilliseconds = 3000;
        });
        services.AddSingleton<RetryModule>();

        var serviceProvider = services.BuildServiceProvider();
        var retryModule = serviceProvider.GetRequiredService<RetryModule>();
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var config = retryModule.GetModuleConfig(moduleIdentifier);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsInstanceOfType(config, typeof(RetryModuleConfig));
        var retryConfig = (RetryModuleConfig)config;
        Assert.AreEqual(7, retryConfig.RetryCount);
        Assert.AreEqual(3000, retryConfig.DelayMilliseconds);
    }

    [TestMethod]
    public void AuditModule_ShouldFallbackToLegacyOptions_WhenOptionsPatternNotRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        
        // Register legacy profile-based options
        var auditOptions = new AuditModuleOptions { LogLevel = "Warning" };
        
        services.AddSingleton<IModuleGlobalOptionsAccessor<AuditModuleOptions>>(
            sp => new ModuleGlobalOptionsAccessor<AuditModuleOptions>(new Dictionary<string, AuditModuleOptions> 
            { 
                ["Default"] = auditOptions 
            }));
        services.AddSingleton<AuditModule>();

        var serviceProvider = services.BuildServiceProvider();
        var auditModule = serviceProvider.GetRequiredService<AuditModule>();
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var config = auditModule.GetModuleConfig(moduleIdentifier);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsInstanceOfType(config, typeof(AuditModuleConfig));
        var auditConfig = (AuditModuleConfig)config;
        Assert.AreEqual("Warning", auditConfig.LogLevel);
    }

    [TestMethod]
    public void Module_ShouldSupportAttributeOverrides_WithOptionsPattern()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceRetryOptions(options =>
        {
            options.RetryCount = 3;
            options.DelayMilliseconds = 1000;
        });

        // Add attribute-specific override using dictionary approach for netstandard2.0 compatibility
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");
        var moduleConfig = new ModuleConfig("RetryModuleAttribute");
        moduleConfig.SetModuleProperty("RetryCount", 10);
        
        // Create a keyed service dictionary for netstandard2.0 compatibility
        var keyedServices = new Dictionary<ModuleIdentifier, ModuleConfig>
        {
            [moduleIdentifier] = moduleConfig
        };
        services.AddSingleton<Dictionary<ModuleIdentifier, ModuleConfig>>(keyedServices);
        
        services.AddSingleton<RetryModule>();

        var serviceProvider = services.BuildServiceProvider();
        var retryModule = serviceProvider.GetRequiredService<RetryModule>();

        // Act
        var config = retryModule.GetModuleConfig(moduleIdentifier);

        // Assert
        Assert.IsNotNull(config);
        Assert.IsInstanceOfType(config, typeof(RetryModuleConfig));
        var retryConfig = (RetryModuleConfig)config;
        Assert.AreEqual(10, retryConfig.RetryCount); // Overridden by attribute
        Assert.AreEqual(1000, retryConfig.DelayMilliseconds); // From base options
    }

    [TestMethod]
    public void Module_ShouldCacheConfigurationInstances()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceAuditOptions(options =>
        {
            options.LogLevel = "Information";
        });
        services.AddSingleton<AuditModule>();

        var serviceProvider = services.BuildServiceProvider();
        var auditModule = serviceProvider.GetRequiredService<AuditModule>();
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var config1 = auditModule.GetModuleConfig(moduleIdentifier);
        var config2 = auditModule.GetModuleConfig(moduleIdentifier);

        // Assert
        Assert.AreSame(config1, config2, "Configuration instances should be cached");
    }

    [TestMethod]
    public void Module_ShouldSupportDifferentProfileConfigurations()
    {
        // Arrange
        var services = new ServiceCollection();
        // For this test, we'll simulate profile-based configuration manually
        var defaultOptions = new RetryModuleOptions { RetryCount = 3, DelayMilliseconds = 1000 };
        var aggressiveOptions = new RetryModuleOptions { RetryCount = 10, DelayMilliseconds = 500 };
        
        services.AddSingleton<IModuleGlobalOptionsAccessor<RetryModuleOptions>>(
            sp => new ModuleGlobalOptionsAccessor<RetryModuleOptions>(new Dictionary<string, RetryModuleOptions>
            {
                ["Default"] = defaultOptions,
                ["Aggressive"] = aggressiveOptions
            }));
        services.AddSingleton<RetryModule>();

        var serviceProvider = services.BuildServiceProvider();
        var retryModule = serviceProvider.GetRequiredService<RetryModule>();
        
        var handleIdentifier = HandleIdentifier.From<TestRequest, TestResponse>("TestMethod");
        var defaultIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");
        var aggressiveIdentifier = ModuleIdentifier.From(handleIdentifier, "Aggressive");

        // Act
        var defaultConfig = (RetryModuleConfig)retryModule.GetModuleConfig(defaultIdentifier);
        var aggressiveConfig = (RetryModuleConfig)retryModule.GetModuleConfig(aggressiveIdentifier);

        // Assert
        Assert.AreEqual(3, defaultConfig.RetryCount);
        Assert.AreEqual(1000, defaultConfig.DelayMilliseconds);
        
        Assert.AreEqual(10, aggressiveConfig.RetryCount);
        Assert.AreEqual(500, aggressiveConfig.DelayMilliseconds);
    }
}
