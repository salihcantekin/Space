```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                             | Mean     | Error    | StdDev   | Allocated |
|----------------------------------- |---------:|---------:|---------:|----------:|
| Space_Publish_Parallel_Inline      | 31.31 ns | 0.137 ns | 0.128 ns |         - |
| Space_Publish_Parallel_TaskWhenAll | 32.30 ns | 0.104 ns | 0.086 ns |         - |
