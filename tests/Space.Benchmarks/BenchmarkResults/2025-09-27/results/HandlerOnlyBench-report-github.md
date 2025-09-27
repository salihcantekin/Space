```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26100.6584/24H2/2024Update/HudsonValley)
Unknown processor
.NET SDK 10.0.100-rc.1.25451.107
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  34.58 ns | 0.265 ns | 0.221 ns |      - |         - |
| Mediator_Send |  17.95 ns | 0.218 ns | 0.204 ns | 0.0014 |      24 B |
| MediatR_Send  | 259.37 ns | 5.160 ns | 5.735 ns | 0.0901 |    1512 B |
