```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-BVLAOW : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

IterationCount=15  WarmupCount=5  

```
| Method                  | Mean     | Error    | StdDev   | Allocated |
|------------------------ |---------:|---------:|---------:|----------:|
| Space_Send_WithPipeline | 52.25 ns | 0.587 ns | 0.549 ns |         - |
