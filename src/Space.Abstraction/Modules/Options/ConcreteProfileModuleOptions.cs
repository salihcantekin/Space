using System;
using System.Collections.Generic;

namespace Space.Abstraction.Modules.Options;

/// <summary>
/// Concrete implementation of ProfileModuleOptions for use in dependency injection.
/// </summary>
/// <typeparam name="T">The module options type</typeparam>
internal class ConcreteProfileModuleOptions<T> : ProfileModuleOptions<T> where T : BaseModuleOptions, new()
{
    // This class inherits all functionality from ProfileModuleOptions<T>
    // It's needed because ProfileModuleOptions<T> is abstract
}
