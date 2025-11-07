```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6725/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  52.62 ns | 1.005 ns | 1.032 ns |      - |         - |
| Mediator_Send_WithBehavior |  21.77 ns | 0.452 ns | 0.555 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 315.88 ns | 4.875 ns | 4.321 ns | 0.0973 |    1632 B |
