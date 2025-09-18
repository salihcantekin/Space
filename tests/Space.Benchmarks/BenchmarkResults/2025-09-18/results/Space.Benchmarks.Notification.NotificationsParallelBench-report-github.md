```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                             | Mean     | Error    | StdDev   | Allocated |
|----------------------------------- |---------:|---------:|---------:|----------:|
| Space_Publish_Parallel_Inline      | 29.82 ns | 0.240 ns | 0.212 ns |         - |
| Space_Publish_Parallel_TaskWhenAll | 31.26 ns | 0.171 ns | 0.134 ns |         - |
