using System.Diagnostics;
using System.Text.Json;
using SpaceTradersAPI.App;
using SpaceTradersAPI.App.Responses;

var accounts = await ReadAccounts(ReadFile());

var ship = (await accounts.Selected.SelectedAgent.API.GetShip("FAUSTVX-3")).ValueOrThrow;
Console.WriteLine(await ship.CreateChart());
// Console.WriteLine(ship);
Console.WriteLine((await (ship.Nav.Status != "DOCKED" ? ship.Dock() : ship.Orbit())).ValueOrThrow);

static async Task<Account> ReadAccounts(FileInfo accountsFile)
{
    using var stream = accountsFile.OpenRead();
    var accounts = JsonSerializer.Deserialize<Account>(stream, new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
    Console.WriteLine((await accounts.API.GetServerStatus()).ValueOrThrow);
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
