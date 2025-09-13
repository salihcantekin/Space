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
| Typed_Unnamed    | 33.12 ns | 0.447 ns | 0.419 ns |  1.00 |    0.02 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 34.21 ns | 0.472 ns | 0.442 ns |  1.03 |    0.02 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 61.26 ns | 0.745 ns | 0.697 ns |  1.85 |    0.03 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 65.28 ns | 0.772 ns | 0.722 ns |  1.97 |    0.03 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 49.14 ns | 0.601 ns | 0.562 ns |  1.48 |    0.02 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 48.36 ns | 0.708 ns | 0.662 ns |  1.46 |    0.03 | 0.0014 |      24 B |        1.00 |
