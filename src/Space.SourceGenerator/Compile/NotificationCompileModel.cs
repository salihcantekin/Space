namespace Space.SourceGenerator.Compile;

public record NotificationCompileModel : BaseCompileModel
{
    public override string ToString() => $"{ClassFullName}.{MethodName}";
}