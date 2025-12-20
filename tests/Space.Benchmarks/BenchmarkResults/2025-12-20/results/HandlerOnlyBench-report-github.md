```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  31.78 ns | 0.309 ns | 0.241 ns |      - |         - |
| Mediator_Send |  17.61 ns | 0.240 ns | 0.224 ns | 0.0014 |      24 B |
| MediatR_Send  | 251.62 ns | 3.656 ns | 3.419 ns | 0.0901 |    1512 B |
