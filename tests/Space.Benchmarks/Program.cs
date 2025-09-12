using BenchmarkDotNet.Running;

public static class Program
{
    public static void Main(string[] args)
    {
        // Run all benchmarks in this assembly
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}
