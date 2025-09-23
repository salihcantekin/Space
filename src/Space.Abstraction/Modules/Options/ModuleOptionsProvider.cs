using Microsoft.Extensions.Options;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Space.Abstraction.Modules.Options;

/// <summary>
/// Default implementation of IModuleOptionsProvider that integrates with Microsoft.Extensions.Options
/// and supports attribute-based overrides and profile-based configuration.
/// </summary>
/// <typeparam name="TOptions">The strongly-typed options class</typeparam>
public class ModuleOptionsProvider<TOptions>(
    IOptionsMonitor<TOptions> optionsMonitor,
    IServiceProvider serviceProvider) : IModuleOptionsProvider<TOptions>
    where TOptions : class, new()
{
    private readonly IOptionsMonitor<TOptions> _optionsMonitor = optionsMonitor ?? throw new ArgumentNullException(nameof(optionsMonitor));
    private readonly IServiceProvider _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public TOptions GetOptions(ModuleIdentifier moduleIdentifier)
    {
        // ModuleIdentifier is a struct, so no null check needed

        // Start with base options from IOptionsMonitor (configured via standard .NET Options pattern)
        var baseOptions = _optionsMonitor.CurrentValue;

        // Apply profile-specific overrides if available
        var profileOptions = GetProfileOptions(moduleIdentifier.ProfileName);

        // Apply attribute-specific overrides
        var attributeOptions = GetAttributeOptions(moduleIdentifier);

        // Merge all options with proper precedence: base -> profile -> attribute
        return MergeOptions(baseOptions, profileOptions, attributeOptions);
    }

    public TOptions GetOptions(string profileName = null)
    {
        // Start with base options from IOptionsMonitor
        var baseOptions = _optionsMonitor.CurrentValue;

        // Apply profile-specific overrides if available
        var profileOptions = GetProfileOptions(profileName);

        // Merge base and profile options
        return MergeOptions(baseOptions, profileOptions, null);
    }

    private TOptions GetProfileOptions(string profileName)
    {
        // Early return if TOptions doesn't extend BaseModuleOptions
        if (!typeof(BaseModuleOptions).IsAssignableFrom(typeof(TOptions)))
        {
            return null;
        }

        var globalOptionsAccessorType = typeof(IModuleGlobalOptionsAccessor<>).MakeGenericType(typeof(TOptions));
        var globalOptionsAccessor = _serviceProvider.GetService(globalOptionsAccessorType);

        // Early return if no global options accessor is available
        if (globalOptionsAccessor == null)
        {
            return null;
        }

        var profilesProperty = globalOptionsAccessorType.GetProperty("Profiles");
        var profiles = profilesProperty?.GetValue(globalOptionsAccessor);

        // Early return if profiles property is null
        if (profiles == null)
        {
            return null;
        }

        // The profiles should be IReadOnlyDictionary<string, TOptions>
        var profilesDictType = typeof(IReadOnlyDictionary<,>).MakeGenericType(typeof(string), typeof(TOptions));

        // Early return if profiles is not the expected dictionary type
        if (!profilesDictType.IsAssignableFrom(profiles.GetType()))
        {
            return null;
        }

        var normalizedProfileName = string.IsNullOrWhiteSpace(profileName) ? "Default" : profileName;
        var tryGetValueMethod = profiles.GetType().GetMethod("TryGetValue");
        var parameters = new object[] { normalizedProfileName, null };
        var found = (bool)tryGetValueMethod.Invoke(profiles, parameters);

        // Early return if profile not found
        if (!found)
        {
            return null;
        }

        return parameters[1] as TOptions;
    }

    private TOptions GetAttributeOptions(ModuleIdentifier moduleIdentifier)
    {
        // Try to get attribute-specific configuration from DI using a workaround for netstandard2.0
        try
        {
            // Check if we have a keyed service registry (custom implementation for netstandard2.0)
            var keyedServiceType = typeof(Dictionary<,>).MakeGenericType(typeof(ModuleIdentifier), typeof(ModuleConfig));

            if (_serviceProvider.GetService(keyedServiceType) is IDictionary keyedServices && keyedServices.Contains(moduleIdentifier))
            {
                if (keyedServices[moduleIdentifier] is ModuleConfig moduleConfig)
                {
                    return ConvertModuleConfigToOptions(moduleConfig);
                }
            }

            // Fallback: try to get a single ModuleConfig (less precise but works for basic scenarios)
            if (_serviceProvider.GetService(typeof(ModuleConfig)) is ModuleConfig singleModuleConfig && singleModuleConfig.ModuleName == moduleIdentifier.HandleIdentifier.Name)
            {
                return ConvertModuleConfigToOptions(singleModuleConfig);
            }
        }
        catch
        {
            // Ignore errors and return null
        }

        return null;
    }

    private TOptions ConvertModuleConfigToOptions(ModuleConfig moduleConfig)
    {
        var options = new TOptions();
        var properties = moduleConfig.GetAllModuleProperties();

        if (properties.Count == 0)
        {
            return options;
        }

        // Use reflection to map properties from dictionary to strongly-typed options
        var optionsType = typeof(TOptions);
        foreach (var property in properties)
        {
            var propertyInfo = optionsType.GetProperty(property.Key);
            if (propertyInfo != null && propertyInfo.CanWrite)
            {
                try
                {
                    var convertedValue = Convert.ChangeType(property.Value, propertyInfo.PropertyType);
                    propertyInfo.SetValue(options, convertedValue);
                }
                catch
                {
                    // Ignore conversion errors - keep default value
                }
            }
        }

        return options;
    }

    private TOptions MergeOptions(TOptions baseOptions, TOptions profileOptions, TOptions attributeOptions)
    {
        // Create a new instance to avoid modifying the original
        var result = new TOptions();

        // Copy properties with precedence: attribute > profile > base
        var optionsType = typeof(TOptions);
        var properties = optionsType.GetProperties();

        foreach (var property in properties)
        {
            if (!property.CanWrite || !property.CanRead)
                continue;

            object value = null;

            // Check attribute options first (highest priority)
            if (attributeOptions != null)
            {
                var attributeValue = property.GetValue(attributeOptions);
                if (attributeValue != null && !IsDefaultValue(attributeValue, property.PropertyType))
                {
                    value = attributeValue;
                }
            }

            // Check profile options second
            if (value == null && profileOptions != null)
            {
                var profileValue = property.GetValue(profileOptions);
                if (profileValue != null && !IsDefaultValue(profileValue, property.PropertyType))
                {
                    value = profileValue;
                }
            }

            // Use base options as fallback
            if (value == null && baseOptions != null)
            {
                value = property.GetValue(baseOptions);
            }

            if (value != null)
            {
                property.SetValue(result, value);
            }
        }

        return result;
    }

    private static bool IsDefaultValue(object value, Type type)
    {
        if (value == null)
            return true;

        if (type.IsValueType)
        {
            return value.Equals(Activator.CreateInstance(type));
        }

        return false;
    }
}
