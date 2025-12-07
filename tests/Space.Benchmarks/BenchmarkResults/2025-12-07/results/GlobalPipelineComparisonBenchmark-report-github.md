```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  Job-YAQSQI : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI

Runtime=.NET 8.0  IterationCount=20  LaunchCount=1  
WarmupCount=5  

```
| Method                 | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|----------------------- |----------:|------:|-------:|----------:|------------:|
| &#39;Space GlobalPipeline&#39; |  33.70 ns |  1.00 | 0.0014 |      24 B |        1.00 |
| &#39;MediatR Behavior&#39;     | 454.71 ns | 13.49 | 0.1116 |    1872 B |       78.00 |
| &#39;Mediator Behavior&#39;    |  68.17 ns |  2.02 | 0.0014 |      24 B |        1.00 |
