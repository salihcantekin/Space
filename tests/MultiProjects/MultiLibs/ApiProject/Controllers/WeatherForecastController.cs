using Microsoft.AspNetCore.Mvc;
using Space.Abstraction;

namespace ApiProject.Controllers;

[ApiController]
[Route("[controller]")]
public class WeatherForecastController(ISpace space) : ControllerBase
{
    [HttpGet]
    public async Task Get()
    {
        var req = new Lib1.CreateUserCommand("TestUser");
        var res = await space.Send(req);
        Console.WriteLine($"User created with ID: {res.UserId}");
    }
}
