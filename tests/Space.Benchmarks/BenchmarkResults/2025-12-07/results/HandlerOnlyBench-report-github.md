```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method        | Mean      | Error    | StdDev   | Gen0   | Allocated |
|-------------- |----------:|---------:|---------:|-------:|----------:|
| Space_Send    |  36.91 ns | 0.203 ns | 0.169 ns |      - |         - |
| Mediator_Send |  17.65 ns | 0.274 ns | 0.242 ns | 0.0014 |      24 B |
| MediatR_Send  | 244.40 ns | 2.025 ns | 1.581 ns | 0.0901 |    1512 B |
