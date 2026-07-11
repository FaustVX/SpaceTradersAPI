using System.Text.Json;
using SpaceTradersAPI.App;
using SpaceTradersAPI.App.Responses;

var accounts = await ReadAccounts(ReadFile());

var ship = await accounts.Selected.SelectedAgent.API.GetShip("FAUSTVX-3").ValueOrThrowAsync();
Console.WriteLine(ship);
var contract = await ship.NegociateContract().MapErrorAsync(e => ship.GetAgent().API.GetContract(e.Data["contractId"]!.GetValue<string>())).ValueOrThrowAsync();
Console.WriteLine(contract);
Console.WriteLine(await contract.AcceptContract().ValueOrThrowAsync());

static async Task<Account> ReadAccounts(FileInfo accountsFile)
{
    using var stream = accountsFile.OpenRead();
    var accounts = JsonSerializer.Deserialize<Account>(stream, new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
    Console.WriteLine(await accounts.API.GetServerStatus().ValueOrThrowAsync());
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

    extension<T>(T @enum)
    where T : struct, Enum
    {
        public string ToUpperCase()
        => @enum.ToString().ToUpperInvariant();
    }
}
