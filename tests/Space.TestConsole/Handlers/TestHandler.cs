using Space.Abstraction;
using Space.Abstraction.Attributes;
using Space.Abstraction.Context;
using Space.Abstraction.Contracts;
using Space.Abstraction.Modules.Audit;

namespace Space.TestConsole;

public class TestHandler : IHandler<UserCreateCommand, UserCreateResponse>
{
    [Handle(Name = "NothingHandle")]
    public ValueTask Handle(HandlerContext<int> ctx)
    {
        Log.Add($"[TestHandler.int] Processing: {ctx.Request}");
        return new ValueTask();
    }

    [Pipeline(Order = 1)]
    public async ValueTask HandlePipeline(PipelineContext<int> ctx, PipelineDelegate<int, Nothing> next)
    {
        Log.Add($"   [HandlePipeline1] Processing Id: {ctx.Request} BEFORE");
        var val = await next(ctx);
        Log.Add($"   [HandlePipeline1] Processing Id: {ctx.Request} AFTER");
    }

    private int counter = 0;
    UserCreateResponse res = new("");

    [Handle(IsDefault = true)]
    [AuditModule(Provider = typeof(NullAuditModuleProvider))]
    public ValueTask<UserCreateResponse> Handle(HandlerContext<UserCreateCommand> ctx)
    {
        Log.Add($"[TestHandler.UserCreateCommand] Processing: {ctx.Request.Email}");

        if (counter < 2)
        {
            counter++;
            Log.Add($"   [TestHandler.UserCreateCommand] Throwing exception for {ctx.Request.Email}_{ctx.Request.Name}");
            //throw new Exception("Random exception");
        }

        var result = new UserCreateResponse($"User created: {ctx.Request.Email}");
        return ValueTask.FromResult(result);
    }
}
