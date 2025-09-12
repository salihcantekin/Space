```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  52.65 ns | 0.694 ns | 0.649 ns |      - |         - |
| Mediator_Send_WithBehavior |  20.35 ns | 0.184 ns | 0.172 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 273.18 ns | 4.822 ns | 6.270 ns | 0.0973 |    1632 B |
