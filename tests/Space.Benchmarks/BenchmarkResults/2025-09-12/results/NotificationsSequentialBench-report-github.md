```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method           | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------- |---------:|---------:|---------:|-------:|----------:|
| Space_Publish    | 42.74 ns | 0.301 ns | 0.251 ns |      - |         - |
| Mediator_Publish | 19.35 ns | 0.149 ns | 0.124 ns |      - |         - |
| MediatR_Publish  | 76.07 ns | 0.820 ns | 0.727 ns | 0.0238 |     400 B |
