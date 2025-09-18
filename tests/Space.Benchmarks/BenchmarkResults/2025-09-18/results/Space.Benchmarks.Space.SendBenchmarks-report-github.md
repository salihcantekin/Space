```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Typed_Unnamed    | 35.20 ns | 0.382 ns | 0.339 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 37.85 ns | 0.482 ns | 0.451 ns |  1.08 |    0.02 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 58.28 ns | 1.109 ns | 1.038 ns |  1.66 |    0.03 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 61.32 ns | 0.647 ns | 0.606 ns |  1.74 |    0.02 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 56.98 ns | 0.897 ns | 0.839 ns |  1.62 |    0.03 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 58.31 ns | 0.423 ns | 0.395 ns |  1.66 |    0.02 | 0.0014 |      24 B |        1.00 |
