```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Typed_Unnamed    | 34.76 ns | 1.064 ns | 0.995 ns |  1.00 |    0.04 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 42.54 ns | 1.005 ns | 0.940 ns |  1.22 |    0.04 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 56.35 ns | 2.242 ns | 2.097 ns |  1.62 |    0.07 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 55.79 ns | 1.737 ns | 1.625 ns |  1.61 |    0.06 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 48.13 ns | 1.563 ns | 1.220 ns |  1.39 |    0.05 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 63.02 ns | 1.996 ns | 1.770 ns |  1.81 |    0.07 | 0.0014 |      24 B |        1.00 |
