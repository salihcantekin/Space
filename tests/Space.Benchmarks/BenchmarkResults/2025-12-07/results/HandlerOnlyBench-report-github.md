```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  33.15 ns | 0.060 ns | 0.053 ns |      - |         - |
| Mediator_Send |  17.66 ns | 0.085 ns | 0.076 ns | 0.0014 |      24 B |
| MediatR_Send  | 240.09 ns | 1.379 ns | 1.151 ns | 0.0901 |    1512 B |
