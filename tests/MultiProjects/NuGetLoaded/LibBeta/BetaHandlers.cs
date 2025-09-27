using LibAlpha;
using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts; // reuse AlphaPing

namespace LibBeta;

public record BetaQuery(int Id) : IRequest<BetaResult>;
public record BetaResult(string Value);

public class BetaHandlers : IHandler<BetaQuery, BetaResult>, IHandler<AlphaPing, AlphaPong>
{
    [Handle(IsDefault = true)]
    public ValueTask<BetaResult> Handle(HandlerContext<BetaQuery> ctx)
    {
        return ValueTask.FromResult(new BetaResult($"Beta:{ctx.Request.Id}"));
    }

    // override AlphaPing handler so aggregator merges multiple projects
    [Handle(IsDefault = false, Name = "BetaAlphaPing")]
    public ValueTask<AlphaPong> Handle(HandlerContext<AlphaPing> ctx)
    {
        return ValueTask.FromResult(new AlphaPong($"BetaOverride:{ctx.Request.Message}"));
    }
}
