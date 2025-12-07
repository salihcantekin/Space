```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                             | Mean     | Error    | StdDev   | Allocated |
|----------------------------------- |---------:|---------:|---------:|----------:|
| Space_Publish_Parallel_Inline      | 29.20 ns | 0.092 ns | 0.086 ns |         - |
| Space_Publish_Parallel_TaskWhenAll | 30.26 ns | 0.106 ns | 0.099 ns |         - |
