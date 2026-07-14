using System.Text.Json;
using SpaceTradersAPI.Lib;
using SpaceTradersAPI.Lib.Models;
using SpaceTradersAPI.Lib.Responses;

var accounts = await ReadAccounts(ReadFile());

if (accounts.Selected.Agents is [])
{
    var registration = await accounts.Selected.API.RegisterAgent("FAUSTVX", V2.FactionSymbol.Galactic).ValueOrThrowAsync();
    Console.WriteLine(registration.Agent.HeadQuarters);
    accounts.Selected.SelectedAgent = accounts.Selected.Agents[0];
    var ship = await accounts.Selected.SelectedAgent.API.GetShip("FAUSTVX-2").ValueOrThrowAsync();
    Console.WriteLine(ship);
    var shipPos = await ship.Nav.WaypointSymbol.GetWaypointData().ValueOrThrowAsync();
    (V2.Waypoint, long) closerShipyard = (default!, long.MaxValue);
    await foreach (var loc in accounts.API.ListWaypointInSystem(ship.Nav.WaypointSymbol.System, traits: [V2.WaypointTraitSymbol.Shipyard]))
        if (loc.DistanceWithSquared(shipPos) is var dist && dist < closerShipyard.Item2)
            closerShipyard = (loc, dist);
    Console.WriteLine(closerShipyard);
    await ship.Orbit();
    await ship.Navigate(closerShipyard.Item1.Symbol);
}
{
    var ship = await accounts.Selected.SelectedAgent.API.GetShip("FAUSTVX-1").ValueOrThrowAsync();
    Console.WriteLine($"Moving to {ship.Nav.Route}");
    await ship.GetNav().Await();
    Console.WriteLine("Arrived");
}

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
