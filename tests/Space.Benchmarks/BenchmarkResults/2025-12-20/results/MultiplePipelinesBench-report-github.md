```

BenchmarkDotNet v0.15.2, Windows 11 (10.0.26200.7309)
Unknown processor
.NET SDK 10.0.101
  [Host]     : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI
  DefaultJob : .NET 8.0.19 (8.0.1925.36514), X64 RyuJIT AVX-512F+CD+BW+DQ+VL+VBMI


```
| Method                   | Mean      | Gen0   | Allocated |
|------------------------- |----------:|-------:|----------:|
| &#39;Space (2 pipes)&#39;        |  38.34 ns |      - |         - |
| &#39;Mediator (2 behaviors)&#39; |  12.04 ns | 0.0014 |      24 B |
| &#39;MediatR (2 behaviors)&#39;  | 272.24 ns | 0.1001 |    1680 B |
| &#39;Space (3 pipes)&#39;        |  41.22 ns |      - |         - |
| &#39;Mediator (3 behaviors)&#39; |  13.62 ns | 0.0014 |      24 B |
| &#39;MediatR (3 behaviors)&#39;  | 318.17 ns | 0.1073 |    1800 B |
