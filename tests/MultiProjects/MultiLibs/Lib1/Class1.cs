using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;

namespace Lib1;

public class TestHandlers
{
    [Handle]
    public ValueTask<CreateUserResponse> Handle(HandlerContext<CreateUserCommand> ctx)
    {
        return ValueTask.FromResult(new CreateUserResponse(ctx.Request + "_" + Guid.NewGuid()));
    }
}



public record CreateUserResponse(string UserId);
public record CreateUserCommand(string UserName) : IRequest<CreateUserResponse>;
