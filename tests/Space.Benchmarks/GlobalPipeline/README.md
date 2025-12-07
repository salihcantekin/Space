# Global Pipeline Benchmarks

Bu benchmark suite, Space'in Global Pipeline özelli?inin performans karakteristiklerini ölçer.

## Mevcut Benchmarks

### 1. GlobalPipelineOverheadBenchmark
**Amaç**: Global Pipeline kullan?m?n?n performans overhead'ini ölçme

**Test Senaryosu**:
- **WithoutGlobalPipeline** (Baseline): Sade handler, global pipeline yok
- **WithGlobalPipeline**: Handler + 1 global pipeline

**Beklenen Sonuç**: Global pipeline overhead minimal olmal? (~5-10%)

### 2. MultipleGlobalPipelinesBenchmark
**Amaç**: Birden fazla global pipeline'?n overhead'ini ölçme

**Test Senaryolar?**:
- **Single_GlobalPipeline** (Baseline): 1 global pipeline
- **Multiple_GlobalPipelines**: 3 global pipeline (farkl? ExecutionStage'lerde)

**Beklenen Sonuç**: Her ek global pipeline linear overhead eklemeli

## Benchmark Çal??t?rma

### Tüm GlobalPipeline Benchmarks
```bash
dotnet run -c Release --project tests\Space.Benchmarks\Space.Benchmarks.csproj --filter *GlobalPipeline*
```

### Sadece Overhead Benchmark
```bash
dotnet run -c Release --project tests\Space.Benchmarks\Space.Benchmarks.csproj --filter *Overhead*
```

### Sadece Multiple Pipelines Benchmark
```bash
dotnet run -c Release --project tests\Space.Benchmarks\Space.Benchmarks.csproj --filter *Multiple*
```

## Ba?ar? Kriterleri

### 1. GlobalPipelineOverheadBenchmark
- ? WithGlobalPipeline, baseline'dan max %10 daha yava?
- ? Memory allocation fark? minimal (0-32 bytes)
- ? ValueTask-based execution (no Task allocation)

### 2. MultipleGlobalPipelinesBenchmark
- ? Linear scaling: Her ek pipeline ~5-10ns overhead
- ? ExecutionStage overhead minimal
- ? Memory allocation sabit kalmal?

## Örnek Beklenen Sonuçlar

```
BenchmarkDotNet v0.15.2, Windows 11
Intel Core i7-9700K CPU 3.60GHz, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.100

| Method                          | Mean      | Ratio | Allocated  |
|-------------------------------- |----------:|------:|-----------:|
| WithoutGlobalPipeline (Baseline)| 120.0 ns  | 1.00  | -          |
| WithGlobalPipeline              | 130.0 ns  | 1.08  | -          |

| Method                          | Mean      | Ratio | Allocated  |
|-------------------------------- |----------:|------:|-----------:|
| Single_GlobalPipeline (Baseline)| 130.0 ns  | 1.00  | -          |
| Multiple_GlobalPipelines (3)    | 150.0 ns  | 1.15  | -          |
```

## Kar??la?t?rma: Di?er Kütüphaneler

Space'in global pipeline özelli?ini di?er popüler kütüphanelerle kar??la?t?rmak için:

### MediatR IPipelineBehavior
**Farklar**:
- MediatR: Runtime reflection, Task-based
- Space: Compile-time source generation, ValueTask-based

**Beklenen Performans**:
- Space ~2-3x daha h?zl?
- Space ~50-70% daha az memory allocation

### Mediator (Microsoft) IPipelineBehavior
**Farklar**:
- Mediator: Source generated, ValueTask-based
- Space: Source generated, ValueTask-based + ExecutionStage support

**Beklenen Performans**:
- Benzer performans (her ikisi de source generated)
- Space daha fazla flexibility (ExecutionStage, Order)

### Manuel Implementation Kar??la?t?rma

Kendi global pipeline implementasyonunuz varsa:

```csharp
// Manuel implementation
public class ManualGlobalPipeline
{
    public async ValueTask<TResponse> Execute<TRequest, TResponse>(
        TRequest request,
        Func<TRequest, ValueTask<TResponse>> next)
    {
        // Validation
        Validate(request);
        
        // Execute
        var response = await next(request);
        
        // Return
        return response;
    }
}

// Space GlobalPipeline
public class SpaceGlobalValidation
{
    [GlobalPipeline(Order = 10)]
    public async ValueTask<TResponse> Validate<TRequest, TResponse>(
        PipelineContext<TRequest> ctx,
        PipelineDelegate<TRequest, TResponse> next)
        where TRequest : notnull
        where TResponse : notnull
    {
        // Same validation logic
        Validate(ctx.Request);
        return await next(ctx);
    }
}
```

**Space Avantajlar?**:
1. **Zero Boilerplate**: Source generator otomatik registration
2. **Type Safety**: Compile-time type checking
3. **Performance**: Inline optimization, no virtual calls
4. **ExecutionStage**: Fine-grained control over execution order

## Teknik Detaylar

### Source Generator Optimizations

1. **Compile-Time Resolution**
   ```csharp
   // Generated code
   RegGlobalPipeline<UserCreateCommand, UserCreateResponse, GlobalValidationPipeline>(
       new GlobalPipelineConfig(){ Order = 10, ExecutionStage = 0 },
       (gpipe, ctx, next) => gpipe.ValidateRequest<UserCreateCommand, UserCreateResponse>(ctx, next));
   ```

2. **Singleton Instance Caching**
   ```csharp
   if (isSingleton)
   {
       var gpInstance = sp.GetRequiredService<TGlobalPipe>();
       registry.RegisterGlobalPipeline<TReq, TRes>(cfg, (ctx, next) => invoker(gpInstance, ctx, next));
   }
   ```

3. **Direct Method Calls** (no reflection, no delegates)

### ExecutionStage Performance

ExecutionStage'ler compile-time'da sort edilir:
```csharp
var globalPipelines = globalPipelineCompileModels
    .OrderBy(gp => gp.ExecutionStage)  // BeforeHandler, BeforeHandlerInner, etc.
    .ThenBy(gp => gp.Order);            // Order within stage
```

Runtime overhead: **0ns** (already sorted in generated code)

## Sonuç Analizi

### Performance Regression Detection

E?er sonuçlar beklentileri kar??lam?yorsa:

1. **WithoutGlobalPipeline Baseline**: 100-150ns olmal?
   - Daha yüksek ise: Handler/Registry overhead var
   - Daha dü?ük ise: Warm-up yetersiz

2. **WithGlobalPipeline Ratio**: 1.05-1.15 olmal?
   - Daha yüksek ise: Global pipeline overhead fazla
   - Kontrol edilecek: Method inline, allocation

3. **Multiple_GlobalPipelines Ratio**: 1.10-1.25 olmal?
   - Daha yüksek ise: Non-linear scaling
   - Kontrol edilecek: Pipeline chain composition

## Katk?da Bulunma

Benchmark sonuçlar?n?z? payla??n:
1. `BenchmarkResults/` klasörüne sonuçlar?n?z? ekleyin
2. Sisteminizin özelliklerini belirtin (CPU, RAM, OS, .NET Version)
3. Pull request olu?turun

### Örnek Sonuç Format?

```
// BenchmarkResults/2024-01-15_i7-9700K.md

## System Info
- CPU: Intel Core i7-9700K @ 3.60GHz
- RAM: 32GB DDR4 3200MHz
- OS: Windows 11 Pro 23H2
- .NET: 8.0.100

## Results

### GlobalPipelineOverheadBenchmark
| Method                     | Mean    | Ratio | Allocated |
|--------------------------- |--------:|------:|----------:|
| WithoutGlobalPipeline      | 118.2ns | 1.00  |         - |
| WithGlobalPipeline         | 127.5ns | 1.08  |         - |

### MultipleGlobalPipelinesBenchmark
| Method                     | Mean    | Ratio | Allocated |
|--------------------------- |--------:|------:|----------:|
| Single_GlobalPipeline      | 125.1ns | 1.00  |         - |
| Multiple_GlobalPipelines   | 148.3ns | 1.19  |         - |
```

## Lisans

MIT License - Space Project
