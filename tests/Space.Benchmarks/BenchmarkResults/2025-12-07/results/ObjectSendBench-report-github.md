```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.100
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method            | Mean       | Ratio | Gen0   | Allocated | Alloc Ratio |
|------------------ |-----------:|------:|-------:|----------:|------------:|
| &#39;Space Typed&#39;     |  18.985 ns |  1.00 |      - |         - |          NA |
| &#39;Mediator Typed&#39;  |   9.534 ns |  0.50 | 0.0014 |      24 B |          NA |
| &#39;MediatR Typed&#39;   | 224.990 ns | 11.85 | 0.0861 |    1440 B |          NA |
| &#39;Space Object&#39;    |  75.758 ns |  3.99 | 0.0014 |      24 B |          NA |
| &#39;Mediator Object&#39; |  26.120 ns |  1.38 | 0.0014 |      24 B |          NA |
| &#39;MediatR Object&#39;  | 248.759 ns | 13.10 | 0.0944 |    1584 B |          NA |
