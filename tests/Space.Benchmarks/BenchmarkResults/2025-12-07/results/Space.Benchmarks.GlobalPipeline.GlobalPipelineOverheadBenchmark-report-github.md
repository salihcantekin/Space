```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method                | Mean     | Error   | StdDev  | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------- |---------:|--------:|--------:|------:|-------:|----------:|------------:|
| WithoutGlobalPipeline | 114.8 ns | 0.20 ns | 0.17 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| WithGlobalPipeline    | 115.2 ns | 0.49 ns | 0.46 ns |  1.00 | 0.0014 |      24 B |        1.00 |
