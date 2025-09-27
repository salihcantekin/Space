using LibAlpha;
using LibBeta;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Space.Abstraction;
using Space.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Add Space with default options
builder.Services.AddSpace(opt =>
{
    opt.ServiceLifetime = ServiceLifetime.Scoped;
});

// Example: enable in-memory cache module when using NuGet package (uncomment if package referenced)
// builder.Services.AddSpaceInMemoryCache();

var app = builder.Build();

app.MapGet("/alpha/{msg}", async (ISpace space, string msg) =>
{
    var res = await space.Send<AlphaPong>(new AlphaPing(msg));
    return Results.Ok(res.Reply);
});

app.MapGet("/beta/{id:int}", async (ISpace space, int id) =>
{
    var res = await space.Send<BetaResult>(new BetaQuery(id));
    return Results.Ok(res.Value);
});

app.MapGet("/multi/{msg}", async (ISpace space, string msg) =>
{
    // Show both alpha default handler (from LibAlpha) and override (LibBeta) selection by Name
    var defaultRes = await space.Send<AlphaPong>(new AlphaPing(msg));
    // Named dispatch example (if generator exposes method) - placeholder comment for docs
    return Results.Ok(defaultRes.Reply);
});

app.MapGet("/notify/{count:int}", async (ISpace space, int count) =>
{
    for (int i = 0; i < count; i++)
    {
        await space.Publish(i);
    }
    return Results.Ok(new { Published = count });
});

app.Run();
