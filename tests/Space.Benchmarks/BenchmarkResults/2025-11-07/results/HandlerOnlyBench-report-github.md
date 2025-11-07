```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6725/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.2.25502.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev    | Gen0   | Allocated |
|-------------- |----------:|---------:|----------:|-------:|----------:|
| Space_Send    |  39.44 ns | 0.531 ns |  0.497 ns |      - |         - |
| Mediator_Send |  20.42 ns | 0.428 ns |  0.666 ns | 0.0014 |      24 B |
| MediatR_Send  | 284.33 ns | 6.940 ns | 20.353 ns | 0.0901 |    1512 B |
