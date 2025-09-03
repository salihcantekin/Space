namespace Space.SourceGenerator
{
    public static class SourceGenConstants
    {
        public const string HandleAttributeName = "Handle";
        public const string HandleAttributeFullName = $"{NameSpaces.SpaceAbstractionAttributes}.{HandleAttributeName}Attribute";
        public const string PipelineAttributeName = "Pipeline";
        public const string PipelineAttributeFullName = $"{NameSpaces.SpaceAbstractionAttributes}.{PipelineAttributeName}Attribute";
        public const string NotificationAttributeName = "Notification";
        public const string NotificationAttributeFullName = $"{NameSpaces.SpaceAbstractionAttributes}.{NotificationAttributeName}Attribute";


        public const string DuplicateHandlerDiagnosticId = "Space001";
        public const string InvalidHandlerFormatDiagnosticId = "Space002";
        public const string MismatchedRequestTypeDiagnosticId = "Space003";
        public const string MismatchedResponseTypeDiagnosticId = "Space004";
        public const string MissingRequestTypeDiagnosticId = "Space005";
        public const string SpaceRegistryClassName = "SpaceRegistry";


        // Diagnostic IDs for HandleAttributeRule
        public const string HandleInvalidParameterCountDiagnosticId = "HANDLE010";
        public const string HandleInvalidParameterTypeDiagnosticId = "HANDLE011";
        public const string HandleInvalidReturnTypeDiagnosticId = "HANDLE012";

        // Diagnostic messages for HandleAttributeRule
        public const string HandleInvalidParameterCountMessage = "Method with HandleAttribute must have exactly one parameter of type HandlerContext<TRequest>.";
        public const string HandleInvalidParameterTypeMessage = "Parameter must be HandlerContext<TRequest>.";
        public const string HandleInvalidReturnTypeMessage = "Return type must be ValueTask<TResponse>.";

        public static class NameSpaces
        {
            public const string SpaceCore = "Space";
            public const string SpaceAbstraction = "Space.Abstraction";
            public const string SpaceAbstractionAttributes = $"{SpaceAbstraction}.Attributes";
            public const string SpaceAbstractionContext = $"{SpaceAbstraction}.Context";
        }

        public static class Context
        {
            public const string HandlerName = "HandlerContext";
            public const string PipelineName = "PipelineContext";
            public const string NotificationName = "NotificationContext";
        }

        public static class Type
        {
            public const string Task = "Task";
            public const string ValueTask = "ValueTask";
        }

        public static class Contracts
        {
            public const string IHandler = "IHandler";
            public const string IPipelineHandler = "IPipelineHandler";
            public const string INotificationHandler = "INotificationHandler";

            public const string HandleMethodName = "Handle";
            public const string PipelineMethodName = "Invoke";
            public const string NotificationMethodName = "HandleNotification";
        }
    }



}


namespace System.Runtime.CompilerServices
{
    public class IsExternalInit { }
}