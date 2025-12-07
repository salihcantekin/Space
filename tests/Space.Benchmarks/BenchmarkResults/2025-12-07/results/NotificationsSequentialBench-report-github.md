```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method           | Mean     | Error    | StdDev   | Gen0   | Allocated |
|----------------- |---------:|---------:|---------:|-------:|----------:|
| Space_Publish    | 40.08 ns | 0.131 ns | 0.123 ns |      - |         - |
| Mediator_Publish | 18.82 ns | 0.068 ns | 0.063 ns |      - |         - |
| MediatR_Publish  | 72.04 ns | 0.376 ns | 0.314 ns | 0.0238 |     400 B |
