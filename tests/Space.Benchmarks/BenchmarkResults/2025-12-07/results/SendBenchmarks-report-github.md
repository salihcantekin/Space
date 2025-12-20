```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method           | Mean     | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Typed_Unnamed    | 33.35 ns | 0.373 ns | 0.349 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 42.11 ns | 0.847 ns | 0.792 ns |  1.26 |    0.03 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 51.72 ns | 0.099 ns | 0.092 ns |  1.55 |    0.02 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 51.22 ns | 0.173 ns | 0.162 ns |  1.54 |    0.02 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 44.60 ns | 0.175 ns | 0.155 ns |  1.34 |    0.01 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 57.40 ns | 0.390 ns | 0.365 ns |  1.72 |    0.02 | 0.0014 |      24 B |        1.00 |
