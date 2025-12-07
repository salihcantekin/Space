```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  83.08 ns | 0.328 ns | 0.291 ns |      - |         - |
| Mediator_Send_WithBehavior |  18.90 ns | 0.039 ns | 0.032 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 261.47 ns | 2.877 ns | 2.550 ns | 0.0973 |    1632 B |
