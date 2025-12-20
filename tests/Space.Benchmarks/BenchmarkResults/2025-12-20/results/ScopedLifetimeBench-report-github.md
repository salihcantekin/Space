```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                | Mean      | Ratio | Gen0   | Allocated | Alloc Ratio |
|---------------------- |----------:|------:|-------:|----------:|------------:|
| &#39;Space Singleton&#39;     |  19.32 ns |  1.00 |      - |         - |          NA |
| &#39;MediatR (transient)&#39; | 237.12 ns | 12.28 | 0.0861 |    1440 B |          NA |
| &#39;Space Scoped&#39;        | 109.10 ns |  5.65 | 0.0200 |     336 B |          NA |
