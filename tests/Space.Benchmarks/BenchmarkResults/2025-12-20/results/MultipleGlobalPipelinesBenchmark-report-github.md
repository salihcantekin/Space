```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-JYXUWV : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=15  LaunchCount=1  
WarmupCount=5  

```
| Method                   | Mean      | Error    | StdDev   | Ratio | RatioSD | Gen0   | Allocated | Alloc Ratio |
|------------------------- |----------:|---------:|---------:|------:|--------:|-------:|----------:|------------:|
| Single_GlobalPipeline    |  75.38 ns | 0.933 ns | 0.873 ns |  1.00 |    0.02 | 0.0029 |      48 B |        1.00 |
| Multiple_GlobalPipelines | 127.76 ns | 1.699 ns | 1.590 ns |  1.70 |    0.03 | 0.0057 |      96 B |        2.00 |
