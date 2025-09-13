```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  33.88 ns | 0.356 ns | 0.333 ns |      - |         - |
| Mediator_Send |  17.47 ns | 0.101 ns | 0.085 ns | 0.0014 |      24 B |
| MediatR_Send  | 250.66 ns | 4.837 ns | 6.117 ns | 0.0901 |    1512 B |
