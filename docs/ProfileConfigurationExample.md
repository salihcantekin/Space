# Feature #4: Profile-based Module Configuration Example

This document demonstrates how to use the new profile-based module configuration system in Space framework.

## Overview

The profile-based configuration system allows you to:
1. Define global default values for module parameters
2. Use different profiles for different environments (e.g., Development, Production)
3. Override global defaults with explicit attribute properties when needed

## Basic Usage

### 1. Configure Global Module Profiles

```csharp
services.AddSpace(opt =>
{
    // Configure Cache module for Production profile
    opt.ConfigureModuleProfile("CacheModule", "Production", config =>
    {
        config["Duration"] = TimeSpan.FromHours(1).ToString();
        config["Provider"] = "Redis";
        config["MaxSize"] = "10000";
    });

    // Configure Cache module for Development profile  
    opt.ConfigureModuleProfile("CacheModule", "Development", config =>
    {
        config["Duration"] = TimeSpan.FromMinutes(5).ToString();
        config["Provider"] = "Memory";
        config["MaxSize"] = "1000";
    });

    // Configure Audit module for Production
    opt.ConfigureModuleProfile("AuditModule", "Production", config =>
    {
        config["LogLevel"] = "Info";
        config["IncludeStackTrace"] = "false";
        config["MaxLogSize"] = "1000";
    });
});
```

### 2. Use Profile-aware Module Attributes

```csharp
public class ProductHandlers
{
    // Uses Production profile defaults for Cache
    [Handle(Name = "GetProduct")]
    [CacheModule(ProfileName = "Production")]
    public async ValueTask<Product> GetProduct(HandlerContext<GetProductQuery> ctx)
    {
        // Handler logic here
    }

    // Uses Development profile defaults but overrides Duration
    [Handle(Name = "GetProductDetails")]
    [CacheModule(ProfileName = "Development", Duration = "PT10M")] // 10 minutes override
    public async ValueTask<ProductDetails> GetProductDetails(HandlerContext<GetProductDetailsQuery> ctx)
    {
        // Handler logic here
    }

    // Uses Default profile (which may have no defaults, so properties fallback to null/empty)
    [Handle(Name = "CreateProduct")]
    [AuditModule] // No ProfileName specified, uses "Default"
    public async ValueTask<CreateProductResponse> CreateProduct(HandlerContext<CreateProductCommand> ctx)
    {
        // Handler logic here
    }
}
```

## How It Works

### Configuration Precedence

The system follows this precedence order (highest to lowest):

1. **Explicit Attribute Properties** - Properties set directly on the module attribute
2. **Profile Defaults** - Global configuration for the specified profile
3. **Null/Empty** - If no configuration is found

### Example Precedence Resolution

Given this configuration:
```csharp
opt.ConfigureModuleProfile("CacheModule", "Production", config =>
{
    config["Duration"] = "01:00:00"; // 1 hour
    config["Provider"] = "Redis";
    config["MaxSize"] = "10000";
});
```

And this attribute:
```csharp
[CacheModule(ProfileName = "Production", Duration = "00:30:00")] // 30 minutes override
```

The resolved configuration would be:
- `Duration`: "00:30:00" (from attribute - explicit override)
- `Provider`: "Redis" (from profile defaults)
- `MaxSize`: "10000" (from profile defaults)

## Benefits

1. **Reduced Duplication**: Set common defaults once instead of repeating them on every handler
2. **Environment-specific Configuration**: Different profiles for dev, test, prod environments
3. **Flexibility**: Still allows fine-grained overrides when needed
4. **Maintainability**: Centralized configuration management

## Migration

Existing module attributes continue to work without changes. The profile system is additive:

- Attributes without `ProfileName` use the "Default" profile
- If no profile configuration exists, the system behaves as before
- Explicit properties always take precedence over profile defaults

## Technical Implementation

The system works by:

1. **SpaceOptions**: Stores profile configurations in a dictionary keyed by "ModuleName:ProfileName"
2. **ModuleConfig**: Enhanced with profile fallback logic in `GetModuleProperty()`
3. **Source Generator**: Emits code that injects profile defaults into ModuleConfig
4. **Dependency Injection**: Registers SpaceOptions and makes it available to generated code