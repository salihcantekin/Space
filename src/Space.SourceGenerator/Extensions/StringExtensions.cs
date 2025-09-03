namespace Space.SourceGenerator.Extensions;

internal static class StringExtensions
{
    internal static bool IsTaskOrValueTask(this string value)
    {
        return IsTask(value) || IsValueTask(value);
    }

    internal static bool IsValueTask(this string value) => value == SourceGenConstants.Type.ValueTask;
    internal static bool IsTask(this string value) => value == SourceGenConstants.Type.Task;
}
