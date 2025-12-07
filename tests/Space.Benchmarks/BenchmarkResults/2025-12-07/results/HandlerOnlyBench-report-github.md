```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  90.63 ns | 0.380 ns | 0.337 ns |      - |         - |
| Mediator_Send |  17.22 ns | 0.054 ns | 0.045 ns | 0.0014 |      24 B |
| MediatR_Send  | 238.75 ns | 1.966 ns | 1.743 ns | 0.0901 |    1512 B |
