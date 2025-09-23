using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Space.Abstraction;
using Space.Abstraction.Modules;
using Space.Abstraction.Modules.Audit;
using Space.Abstraction.Modules.Options;
using Space.Abstraction.Modules.Retry;
using System;

namespace Space.Tests.Options;

// Test types for the tests
public record Request(string Data);
public record Response(string Message);

[TestClass]
public class ModuleOptionsTests
{
    [TestMethod]
    public void AuditOptions_ShouldResolveFromStandardOptionsPattern()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceAuditOptions(options =>
        {
            options.LogLevel = "Debug";
            options.IncludeRequestDetails = true;
            options.MaxContentLength = 2000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsProvider = serviceProvider.GetRequiredService<IModuleOptionsProvider<AuditOptions>>();

        // Act
        var options = optionsProvider.GetOptions();

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual("Debug", options.LogLevel);
        Assert.IsTrue(options.IncludeRequestDetails);
        Assert.AreEqual(2000, options.MaxContentLength);
    }

    [TestMethod]
    public void RetryOptions_ShouldResolveFromStandardOptionsPattern()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceRetryOptions(options =>
        {
            options.RetryCount = 5;
            options.DelayMilliseconds = 2000;
            options.UseExponentialBackoff = true;
            options.BackoffMultiplier = 1.5;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsProvider = serviceProvider.GetRequiredService<IModuleOptionsProvider<RetryOptions>>();

        // Act
        var options = optionsProvider.GetOptions();

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual(5, options.RetryCount);
        Assert.AreEqual(2000, options.DelayMilliseconds);
        Assert.IsTrue(options.UseExponentialBackoff);
        Assert.AreEqual(1.5, options.BackoffMultiplier);
    }

    [TestMethod]
    public void ModuleOptionsProvider_ShouldSupportAttributeOverrides()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceAuditOptions(options =>
        {
            options.LogLevel = "Information";
            options.MaxContentLength = 1000;
        });

        // Register attribute-specific configuration using a dictionary approach for netstandard2.0 compatibility
        var handleIdentifier = HandleIdentifier.From<Request, Response>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");
        var moduleConfig = new ModuleConfig("AuditModuleAttribute");
        moduleConfig.SetModuleProperty("LogLevel", "Error");
        moduleConfig.SetModuleProperty("MaxContentLength", 500);
        
        // Create a keyed service dictionary for netstandard2.0 compatibility
        var keyedServices = new Dictionary<ModuleIdentifier, ModuleConfig>
        {
            [moduleIdentifier] = moduleConfig
        };
        services.AddSingleton<Dictionary<ModuleIdentifier, ModuleConfig>>(keyedServices);
        
        var serviceProvider = services.BuildServiceProvider();
        var optionsProvider = serviceProvider.GetRequiredService<IModuleOptionsProvider<AuditOptions>>();

        // Act
        var options = optionsProvider.GetOptions(moduleIdentifier);

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual("Error", options.LogLevel); // Overridden by attribute
        Assert.AreEqual(500, options.MaxContentLength); // Overridden by attribute
    }

    [TestMethod]
    public void ModuleOptionsProvider_ShouldFallbackToBaseOptions_WhenNoOverrides()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceRetryOptions(options =>
        {
            options.RetryCount = 3;
            options.DelayMilliseconds = 1000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsProvider = serviceProvider.GetRequiredService<IModuleOptionsProvider<RetryOptions>>();
        var handleIdentifier = HandleIdentifier.From<Request, Response>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var options = optionsProvider.GetOptions(moduleIdentifier);

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual(3, options.RetryCount); // From base options
        Assert.AreEqual(1000, options.DelayMilliseconds); // From base options
    }

    [TestMethod]
    public void ModuleOptionsProvider_ShouldSupportProfileBasedConfiguration()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceModuleOptionsWithProfiles<AuditModuleOptions>(profileOptions =>
        {
            profileOptions.WithDefaultProfile(opt => opt.LogLevel = "Information");
            profileOptions.WithProfile("Debug", opt => opt.LogLevel = "Debug");
        });

        var serviceProvider = services.BuildServiceProvider();
        var optionsProvider = serviceProvider.GetRequiredService<IModuleOptionsProvider<AuditModuleOptions>>();

        // Act
        var defaultOptions = optionsProvider.GetOptions("Default");
        var debugOptions = optionsProvider.GetOptions("Debug");

        // Assert
        Assert.IsNotNull(defaultOptions);
        Assert.AreEqual("Information", defaultOptions.LogLevel);
        
        Assert.IsNotNull(debugOptions);
        Assert.AreEqual("Debug", debugOptions.LogLevel);
    }

    [TestMethod]
    public void ServiceProvider_ShouldResolveModuleOptionsUsingExtensions()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSpaceAuditOptions(options =>
        {
            options.LogLevel = "Warning";
            options.IncludeResponseDetails = true;
        });

        var serviceProvider = services.BuildServiceProvider();
        var handleIdentifier = HandleIdentifier.From<Request, Response>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var options = serviceProvider.GetModuleOptions<AuditOptions>(moduleIdentifier);

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual("Warning", options.LogLevel);
        Assert.IsTrue(options.IncludeResponseDetails);
    }

    [TestMethod]
    public void ServiceProvider_ShouldFallbackToStandardIOptions_WhenNoCustomProviderRegistered()
    {
        // Arrange
        var services = new ServiceCollection();
        services.Configure<AuditOptions>(options =>
        {
            options.LogLevel = "Critical";
            options.MaxContentLength = 3000;
        });

        var serviceProvider = services.BuildServiceProvider();
        var handleIdentifier = HandleIdentifier.From<Request, Response>("TestMethod");
        var moduleIdentifier = ModuleIdentifier.From(handleIdentifier, "Default");

        // Act
        var options = serviceProvider.GetModuleOptions<AuditOptions>(moduleIdentifier);

        // Assert
        Assert.IsNotNull(options);
        Assert.AreEqual("Critical", options.LogLevel);
        Assert.AreEqual(3000, options.MaxContentLength);
    }
}
