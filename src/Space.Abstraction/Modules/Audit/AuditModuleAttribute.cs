using System;

namespace Space.Abstraction.Modules.Audit;

[AttributeUsage(AttributeTargets.Method, Inherited = false)]
public class AuditModuleAttribute : Attribute, ISpaceModuleAttribute
{
}