```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method            | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |----------:|------:|-------:|----------:|------------:|
| &#39;Space Typed&#39;     |  19.75 ns |  1.00 |      - |         - |          NA |
| &#39;Mediator Typed&#39;  |  10.12 ns |  0.51 | 0.0014 |      24 B |          NA |
| &#39;MediatR Typed&#39;   | 245.11 ns | 12.41 | 0.0861 |    1440 B |          NA |
| &#39;Space Object&#39;    |  73.01 ns |  3.70 | 0.0014 |      24 B |          NA |
| &#39;Mediator Object&#39; |  27.84 ns |  1.41 | 0.0014 |      24 B |          NA |
| &#39;MediatR Object&#39;  | 265.66 ns | 13.45 | 0.0944 |    1584 B |          NA |
