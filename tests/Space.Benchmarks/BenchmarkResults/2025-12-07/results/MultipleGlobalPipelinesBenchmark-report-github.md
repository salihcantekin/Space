```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method                   | Mean      | Error    | StdDev   | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|---------:|---------:|------:|-------:|----------:|------------:|
| Single_GlobalPipeline    |  73.02 ns | 0.212 ns | 0.198 ns |  1.00 | 0.0029 |      48 B |        1.00 |
| Multiple_GlobalPipelines | 121.86 ns | 0.716 ns | 0.559 ns |  1.67 | 0.0057 |      96 B |        2.00 |
