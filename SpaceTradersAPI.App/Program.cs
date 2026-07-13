using System.Text.Json;
using SpaceTradersAPI.App;
using SpaceTradersAPI.App.Models;
using SpaceTradersAPI.App.Responses;

var accounts = await ReadAccounts(ReadFile());

if (accounts.Selected.Agents is [])
{
    var registration = await accounts.Selected.API.RegisterAgent("FAUSTVX", V2.FactionSymbol.Galactic).ValueOrThrowAsync();
    Console.WriteLine(registration.Agent.HeadQuarters);
    accounts.Selected.SelectedAgent = accounts.Selected.Agents[0];
}
var ship = await accounts.Selected.SelectedAgent.API.GetShip("FAUSTVX-2").ValueOrThrowAsync();
Console.WriteLine(ship);
var shipPos = await ship.Nav.WaypointSymbol.GetWaypointData().ValueOrThrowAsync();
(V2.Waypoint, long) closerShipyard = (default!, long.MaxValue);
await foreach (var loc in accounts.API.ListWaypointInSystem(ship.Nav.WaypointSymbol.System, traits: [V2.WaypointTraitSymbol.Shipyard]))
{
    // Console.WriteLine(loc);
    if (loc.DistanceWithSquared(shipPos) is var dist && dist < closerShipyard.Item2)
        closerShipyard = (loc, dist);
}
Console.WriteLine(closerShipyard);
return;
await ship.Dock();
var contract = await ship.NegociateContract().MapErrorAsync(e => ship.GetAgent().API.GetContract(e.Data?["contractId"]!.GetValue<string>()!)).ValueOrThrowAsync();
await ship.Orbit();
Console.WriteLine(contract);
var navigation = await ship.Navigate(ship.CreateLocation("F51")).ValueOrThrowAsync();
await navigation.Nav.Route.Await();

static async Task<Account> ReadAccounts(FileInfo accountsFile)
{
    using var stream = accountsFile.OpenRead();
    var accounts = JsonSerializer.Deserialize<Account>(stream, new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
    var status = await accounts.API.GetServerStatus().ValueOrThrowAsync();
    Console.WriteLine(status);
    if (status.Version != "v2.3.0")
        throw new Exception($"Incompatible server version. Should be v2.3.0 but server is {status.Version}");
    foreach (var account in accounts.Accounts)
    {
        account.Accounts = accounts;
        foreach (var agent in account.Agents)
        {
            agent.Account = account;
            agent.Accounts = accounts;
        }
    }

    accounts.File = accountsFile;
    return accounts;
}

static FileInfo ReadFile()
{
    var accountsFile = new FileInfo("Accounts.json");
    if (!accountsFile.Exists)
    {
        File.WriteAllText(accountsFile.FullName, """
        {
            "BaseAddress": "https://api.spacetraders.io/v2",
            "Accounts": [
                {
                    "Name": "",
                    "Token": "",
                    "Agents": []
                },
            ]
        }

        """);
        Environment.Exit(-1);
    }
    return accountsFile;
}

public static class Ext
{
    extension<T>(Task<Result<T>> task)
    {
        public Task<T> ValueOrThrowAsync()
        => task.ContinueWith(t => t.Result.ValueOrThrow, TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<TResult>> MapvalueAsync<TResult>(Func<T, TResult> mapper)
        => task.ContinueWith(t => t.Result.MapValue(mapper), TaskContinuationOptions.ExecuteSynchronously);

        public Task<Result<T>> MapErrorAsync(Func<Error, Task<Result<T>>> errorMapper)
        => task.ContinueWith(t => t.Result.MapError(e => errorMapper(e).Result), TaskContinuationOptions.ExecuteSynchronously);
    }

    extension<T>(IAsyncEnumerable<T> values)
    {
        public async IAsyncEnumerable<TResult> MapValues<TResult>(Func<T, TResult> mapper)
        {
            await foreach (var item in values)
                yield return mapper(item);
        }
    }

    extension<T>(T @enum)
    where T : struct, Enum
    {
        public string ToUpperCase()
        => @enum.ToString().ToUpperInvariant();
    }

    extension(TimeSpan)
    {
        public static TimeSpan Min(TimeSpan val1, TimeSpan val2)
        => val1 <= val2 ? val1 : val2;

        public static TimeSpan Max(TimeSpan val1, TimeSpan val2)
        => val1 >= val2 ? val1 : val2;
    }

    extension(V2.IPosition position)
    {
        public double DistanceWith(V2.IPosition other)
        => Math.Sqrt(position.DistanceWithSquared(other));

        public long DistanceWithSquared(V2.IPosition other)
        => Square(other.X - position.X) + Square(other.Y - position.Y);

        static long Square(int a)
        => a * (long)a;
    }
}
