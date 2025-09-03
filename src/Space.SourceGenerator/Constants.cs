namespace Space.SourceGenerator;

public static class Constants
{
    public static class AttributeNames
    {
        public const string Handle = "Handle";

        public const string CacheModule = "CacheModule";
        public const string CacheModuleFullName = $"{CacheModule}Attribute";
    }

    public static class ModuleNames
    {
        public const string Cache = "CacheModule";
    }

    public static class DiagnosticIds
    {
        public const string DuplicateHandler = "Space001";
        public const string InvalidHandlerFormat = "Space002";
    }

    public static class TypeNames
    {
        public const string HandlerInfo = "HandlerInfo";
        public const string SpaceRegistry = "SpaceRegistry";
        public const string SampleHandler = "SampleHandler";
        public const string HandlerContext = "HandlerContext";
        public const string Nothing = "Nothing";
    }

    public static class PropertyNames
    {
        public const string Handlers = "Handlers";
        public const string ClassName = "ClassName";
        public const string MethodName = "MethodName";
        public const string ParameterType = "ParameterType";
        public const string ReturnType = "ReturnType";
    }
}
