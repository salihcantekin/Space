

using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;

namespace LibAlpha;

public record AlphaPing(string Message) : IRequest<AlphaPong>;
public record AlphaPong(string Reply);

public class AlphaHandlers : IHandler<AlphaPing, AlphaPong>
{
    [Handle(IsDefault = true)]
    public ValueTask<AlphaPong> Handle(HandlerContext<AlphaPing> ctx)
    {
        return ValueTask.FromResult(new AlphaPong($"AlphaHandled:{ctx.Request.Message}"));
    }
}
