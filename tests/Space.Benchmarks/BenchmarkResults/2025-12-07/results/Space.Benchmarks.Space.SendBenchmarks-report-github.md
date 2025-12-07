```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method           | Mean     | Error   | StdDev  | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|----------------- |---------:|--------:|--------:|------:|--------:|-------:|----------:|------------:|
| Typed_Unnamed    | 111.6 ns | 0.80 ns | 0.75 ns |  1.00 |    0.01 | 0.0014 |      24 B |        1.00 |
| Typed_Named      | 182.8 ns | 3.84 ns | 3.40 ns |  1.64 |    0.03 | 0.0014 |      24 B |        1.00 |
| IRequest_Unnamed | 139.8 ns | 0.57 ns | 0.47 ns |  1.25 |    0.01 | 0.0014 |      24 B |        1.00 |
| IRequest_Named   | 190.6 ns | 1.35 ns | 1.26 ns |  1.71 |    0.02 | 0.0014 |      24 B |        1.00 |
| Object_Unnamed   | 143.5 ns | 1.71 ns | 1.60 ns |  1.29 |    0.02 | 0.0014 |      24 B |        1.00 |
| Object_Named     | 210.4 ns | 0.70 ns | 0.65 ns |  1.88 |    0.01 | 0.0014 |      24 B |        1.00 |
