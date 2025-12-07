```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  49.22 ns | 0.134 ns | 0.119 ns |      - |         - |
| Mediator_Send_WithBehavior |  18.97 ns | 0.082 ns | 0.073 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 260.21 ns | 2.172 ns | 1.926 ns | 0.0973 |    1632 B |
