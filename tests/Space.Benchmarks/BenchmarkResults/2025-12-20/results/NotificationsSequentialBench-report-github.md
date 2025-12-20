```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method           | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------- |---------:|---------:|---------:|-------:|----------:|
| Space_Publish    | 40.80 ns | 0.248 ns | 0.220 ns |      - |         - |
| Mediator_Publish | 19.49 ns | 0.215 ns | 0.190 ns |      - |         - |
| MediatR_Publish  | 75.23 ns | 0.760 ns | 0.711 ns | 0.0238 |     400 B |
