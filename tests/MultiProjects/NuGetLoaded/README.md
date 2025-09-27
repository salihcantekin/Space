# NuGetLoaded Manual Test Projects

Bu klasör Space NuGet paketlerinin publish edildikten sonra manuel olarak test edilmesi için haz?rlanm??t?r. Amaç repository içindeki kaynak projelere de?il yay?mlanan paketlere referans vermektir.

## Projeler
- ApiHost (net8.0 Web API) - Root aggregator ( `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>` )
- LibAlpha (class library)
- LibBeta (class library)

LibAlpha ve LibBeta kendi handlerlar?n? içerir. ApiHost bu iki projeyi referans ederek source generator'un root projede tüm handlerlar? toplamas?n? sa?lar.

## Kullan?m
Varsay?lan olarak repo içindeki projelere referans verilir (ProjectReference). Yay?nlanan NuGet paketlerini test etmek için derleme s?ras?nda a?a??daki parametreyi verin:

```
dotnet build -c Release /p:UseLocalSpaceProjects=false
```

Ya da `ApiHost`, `LibAlpha`, `LibBeta` projelerinin `.csproj` dosyalar?ndaki `UseLocalSpaceProjects` özelli?ini false yap?n.

NuGet paket versiyonlar?n? test etmek istedi?iniz gerçek versiyonlarla de?i?tirin (örn: `1.0.0-preview.3`).

## Örnek Çal??t?rma
```
dotnet run --project tests/MultiProjects/NuGetLoaded/ApiHost/ApiHost.csproj
```

### Örnek ?stekler
```
GET http://localhost:5000/alpha/hello        -> AlphaHandled:hello
GET http://localhost:5000/beta/5             -> Beta:5
GET http://localhost:5000/multi/hi           -> AlphaHandled:hi (veya override senaryosu için ileride isimlendirilmi? handler ça?r?s? eklenebilir)
GET http://localhost:5000/notify/3           -> 3 adet notification publish eder
```

## Notlar
- Paket testinde `Space.Modules.InMemoryCache` gibi ekstra modülleri de eklemek isterseniz ilgili PackageReference sat?r?n? aç?n.
- Root projede `<SpaceGenerateRootAggregator>true</SpaceGenerateRootAggregator>` olmas? di?er class library projelerindeki handlerlar?n toplanmas? için gereklidir.
- Handler / Pipeline say?s?n? artt?rarak gerçek senaryolar? manuel test edebilirsiniz.
