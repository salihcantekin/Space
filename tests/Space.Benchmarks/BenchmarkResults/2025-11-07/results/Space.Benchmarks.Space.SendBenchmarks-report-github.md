```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6725/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Typed_Unnamed    | 37.55 ns | 0.913 ns | 0.854 ns |  1.00 |    0.03 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 38.49 ns | 0.490 ns | 0.409 ns |  1.03 |    0.02 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 60.97 ns | 1.138 ns | 1.009 ns |  1.62 |    0.04 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 64.70 ns | 1.383 ns | 1.155 ns |  1.72 |    0.05 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 58.61 ns | 1.890 ns | 1.579 ns |  1.56 |    0.05 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 65.34 ns | 8.110 ns | 7.586 ns |  1.74 |    0.20 | 0.0014 |      24 B |        1.00 |
