namespace Space.SourceGenerator.Compile;

public record NotificationCompileModel(string HandlerName): BaseCompileModel
{
    public override string ToString() => $"{ClassFullName}.{MethodName}";
}