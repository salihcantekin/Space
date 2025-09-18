```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                     | Mean      | Error    | StdDev   | Gen0   | Allocated |
|--------------------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send_WithPipeline    |  51.72 ns | 0.681 ns | 0.637 ns |      - |         - |
| Mediator_Send_WithBehavior |  20.45 ns | 0.196 ns | 0.174 ns | 0.0014 |      24 B |
| MediatR_Send_WithBehavior  | 267.77 ns | 4.087 ns | 3.823 ns | 0.0973 |    1632 B |
