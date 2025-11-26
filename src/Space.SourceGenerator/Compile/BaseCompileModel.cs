namespace Space.SourceGenerator.Compile;

public record BaseCompileModel
{
    private string classFullName;
    public string ClassFullName
    {
        get => classFullName;
        set => classFullName = value;
    }

    public string MethodName { get; set; }

    public string RequestParameterTypeName { get; set; }

    public string ReturnTypeName { get; set; }

    public string ReturnTaskTypeName { get; set; }

    public bool IsVoidLike { get; set; }
}
