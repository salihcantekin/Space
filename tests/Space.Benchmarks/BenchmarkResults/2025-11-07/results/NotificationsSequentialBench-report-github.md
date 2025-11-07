```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6725/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method           | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------- |---------:|---------:|---------:|-------:|----------:|
| Space_Publish    | 45.72 ns | 0.895 ns | 0.748 ns |      - |         - |
| Mediator_Publish | 19.93 ns | 0.315 ns | 0.279 ns |      - |         - |
| MediatR_Publish  | 82.77 ns | 1.523 ns | 1.425 ns | 0.0238 |     400 B |
