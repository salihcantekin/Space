# Module Options Pattern

This document describes the new Options pattern implementation for Space modules, providing a standardized way to configure modules using the .NET Options pattern while maintaining backward compatibility.

## Overview

The Space framework now supports the standard .NET Options pattern (`IOptions<T>`, `IOptionsMonitor<T>`) for module configuration, providing:

- **Strongly-typed configuration** - Type-safe configuration classes instead of dictionary-based properties
- **Standard .NET integration** - Uses familiar `IOptions<T>` and `IOptionsMonitor<T>` interfaces
- **Configuration binding** - Direct binding from `appsettings.json` and other configuration sources
- **Attribute overrides** - Per-handler configuration overrides through attributes
- **Profile support** - Multiple configuration profiles for different scenarios
- **Backward compatibility** - Existing code continues to work unchanged

## Quick Start

### 1. Basic Options Registration

```csharp
services.AddSpaceAuditOptions(options =>
{
    options.LogLevel = "Debug";
    options.IncludeRequestDetails = true;
    options.MaxContentLength = 2000;
});
```

### 2. Configuration Binding

```csharp
// appsettings.json
{
  "AuditOptions": {
    "LogLevel": "Information",
    "IncludeRequestDetails": false,
    "MaxContentLength": 1000
  }
}

// Startup.cs
services.AddSpaceAuditOptions(configuration.GetSection("AuditOptions"));
```

### 3. Using in Modules

```csharp
public class MyHandler
{
    private readonly IModuleOptionsProvider<AuditOptions> _auditOptionsProvider;

    public MyHandler(IModuleOptionsProvider<AuditOptions> auditOptionsProvider)
    {
        _auditOptionsProvider = auditOptionsProvider;
    }

    [Handle]
    [AuditModule] // Uses default configuration
    public async Task<Response> HandleRequest(HandlerContext<Request> context)
    {
        var options = _auditOptionsProvider.GetOptions(context.ModuleIdentifier);
        // Use options...
    }
}
```

## Available Modules

### Audit Module

**Options Class**: `AuditOptions`

```csharp
services.AddSpaceAuditOptions(options =>
{
    options.LogLevel = "Information";           // Log level for audit operations
    options.IncludeRequestDetails = true;       // Include request details in logs
    options.IncludeResponseDetails = false;     // Include response details in logs
    options.MaxContentLength = 1000;            // Maximum content length to log
});
```

### Retry Module

**Options Class**: `RetryOptions`

```csharp
services.AddSpaceRetryOptions(options =>
{
    options.RetryCount = 3;                     // Number of retry attempts
    options.DelayMilliseconds = 1000;           // Delay between retries
    options.UseExponentialBackoff = false;      // Use exponential backoff
    options.MaxDelayMilliseconds = 30000;       // Maximum delay for backoff
    options.BackoffMultiplier = 2.0;            // Backoff multiplier
});
```

## Advanced Configuration

### Profile-Based Configuration

```csharp
services.AddSpaceModuleOptionsWithProfiles<AuditModuleOptions>(profileOptions =>
{
    profileOptions.WithDefaultProfile(opt => 
    {
        opt.LogLevel = "Information";
    });
    
    profileOptions.WithProfile("Debug", opt => 
    {
        opt.LogLevel = "Debug";
    });
    
    profileOptions.WithProfile("Production", opt => 
    {
        opt.LogLevel = "Warning";
    });
});
```

### Custom Module Providers

```csharp
services.AddSpaceAuditOptions<MyCustomAuditProvider>(options =>
{
    options.LogLevel = "Debug";
});

// Or with factory
services.AddSpaceAuditOptions(
    serviceProvider => new MyCustomAuditProvider(serviceProvider.GetRequiredService<ILogger>()),
    options => options.LogLevel = "Debug"
);
```

### Attribute-Level Overrides

Attribute overrides continue to work as before and take precedence over global configuration:

```csharp
[Handle]
[AuditModule(LogLevel = "Error")] // Overrides global LogLevel setting
public async Task<Response> HandleCriticalRequest(HandlerContext<Request> context)
{
    // This handler will use "Error" log level regardless of global configuration
}
```

## Configuration Precedence

Configuration values are resolved in the following order (highest to lowest priority):

1. **Attribute overrides** - Values specified in module attributes
2. **Profile configuration** - Values from the specified profile
3. **Base configuration** - Values from the main options configuration
4. **Default values** - Built-in default values

## Migration Guide

### From Legacy Profile Options

**Before:**
```csharp
services.AddSpaceAudit(options =>
{
    options.WithDefaultProfile(opt => opt.LogLevel = "Information");
    options.WithProfile("Debug", opt => opt.LogLevel = "Debug");
});
```

**After:**
```csharp
services.AddSpaceAuditOptions(options =>
{
    options.LogLevel = "Information"; // This becomes the base configuration
});

// For profile support, use:
services.AddSpaceModuleOptionsWithProfiles<AuditModuleOptions>(profileOptions =>
{
    profileOptions.WithDefaultProfile(opt => opt.LogLevel = "Information");
    profileOptions.WithProfile("Debug", opt => opt.LogLevel = "Debug");
});
```

### From Configuration Dictionary

**Before:**
```csharp
// Manual property mapping from dictionary
var config = new AuditModuleConfig();
config.LogLevel = properties["LogLevel"]?.ToString();
```

**After:**
```csharp
// Automatic binding from configuration
services.AddSpaceAuditOptions(configuration.GetSection("Audit"));

// Or programmatic configuration
services.AddSpaceAuditOptions(options =>
{
    options.LogLevel = "Information";
});
```

## Best Practices

1. **Use strongly-typed options** - Prefer the new `AuditOptions`, `RetryOptions` classes over dictionary-based configuration
2. **Leverage configuration binding** - Bind directly from `appsettings.json` for environment-specific settings
3. **Keep attribute overrides minimal** - Use attributes for handler-specific overrides, not global configuration
4. **Use profiles for scenarios** - Create profiles for different environments or use cases
5. **Test configuration resolution** - Write tests to verify your configuration is resolved correctly

## Backward Compatibility

The new Options pattern is fully backward compatible:

- Existing code using `AddSpaceAudit()` and `AddSpaceRetry()` continues to work
- Legacy profile-based configuration is still supported
- Attribute overrides work with both old and new approaches
- Modules automatically detect and use the appropriate configuration method

## Performance Considerations

- **Configuration caching** - Module configurations are cached per module identifier
- **Options monitoring** - Use `IOptionsMonitor<T>` for configuration that may change at runtime
- **Minimal overhead** - New pattern adds minimal overhead compared to legacy approach

## Troubleshooting

### Common Issues

1. **Options not resolving** - Ensure you've registered the options using `AddSpaceModuleOptions<T>()`
2. **Attribute overrides not working** - Verify the module identifier is correctly registered with keyed services
3. **Profile not found** - Check profile names match exactly (case-sensitive)

### Debugging Configuration

```csharp
// Get resolved options for debugging
var options = serviceProvider.GetModuleOptions<AuditOptions>(moduleIdentifier);
Console.WriteLine($"Resolved LogLevel: {options.LogLevel}");
```

## Examples

See the test files for comprehensive examples:
- `Space.Tests/Options/ModuleOptionsTests.cs` - Basic options pattern usage
- `Space.Tests/Options/ModuleIntegrationTests.cs` - Module integration scenarios
