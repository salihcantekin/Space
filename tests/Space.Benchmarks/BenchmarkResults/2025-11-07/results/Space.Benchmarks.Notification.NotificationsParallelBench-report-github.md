```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6725/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                             | Mean     | Error    | StdDev   | Allocated |
|----------------------------------- |---------:|---------:|---------:|----------:|
| Space_Publish_Parallel_Inline      | 34.16 ns | 0.424 ns | 0.396 ns |         - |
| Space_Publish_Parallel_TaskWhenAll | 33.69 ns | 0.645 ns | 0.603 ns |         - |
