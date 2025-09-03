namespace Space.TestConsole.Services;

internal class DataService : IDataService
{
    public string GetFullName() => "Salih Cantekin";

    public int GetRandomNumber() => Random.Shared.Next(1, 100_000);
}
