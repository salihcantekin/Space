```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  77.92 ns | 0.328 ns | 0.307 ns |      - |         - |
| Mediator_Send_WithBehavior |  19.15 ns | 0.062 ns | 0.055 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 266.54 ns | 3.665 ns | 3.428 ns | 0.0973 |    1632 B |
