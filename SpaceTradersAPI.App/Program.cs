using System.Text.Json;
using SpaceTradersAPI.Lib;
using SpaceTradersAPI.Lib.Models;
using SpaceTradersAPI.Lib.Responses;

var accounts = await ReadAccounts(ReadFile());

while(true)
{
    switch (Console.Prompt("SpaceTrader > ").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        case [] or ["--help"]:
            Console.WriteLine("[sel[ect] | list | quit | --help]");
            break;
        case ["sel" or "select", .. var selection]:
            ParseSelect(selection, ["sel[ect]"]);
            break;
        case ["list", .. var selection]:
            ParseList(selection, ["list"]);
            break;
        case ["quit"]:
            Environment.Exit(0);
            break;
    }
}

void ParseSelect(ReadOnlySpan<string> selection, ReadOnlySpan<string> previousCommands)
{
    switch (selection)
    {
        case [] or ["--help"]:
            Console.WriteLine($"{previousCommands.Concat()} [account | agent | ship | --help]");
            break;
        case ["account", .. var account]:
            ParseAccount(account, [..previousCommands, "account"]);
            break;
        case ["agent", .. var agent]:
            ParseAgent(agent, [..previousCommands, "agent"]);
            break;
        case ["ship", .. var ship]:
            ParseShip(ship, [..previousCommands, "ship"]);
            break;
    }

    void ParseAccount(ReadOnlySpan<string> account, ReadOnlySpan<string> previousCommands)
    {
        switch (account)
        {
            case [] or ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [<accountName> | --help]");
                break;
            case [var name]:
                accounts.Selected = AssignAccount(accounts, name);
                break;
        }

        static AccountItem AssignAccount(Account accounts, string name)
        {
            foreach (var a in accounts.Accounts)
            {
                if (a.Name == name)
                    return a;
            }
            (Console.ForegroundColor, var fore) = (ConsoleColor.Red, Console.ForegroundColor);
            Console.WriteLine($"Unknown account <{name}>");
            return accounts.Selected;
        }
    }

    void ParseAgent(ReadOnlySpan<string> agent, ReadOnlySpan<string> previousCommands)
    {
        switch (agent)
        {
            case [] or ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [<agentName> | --help]");
                break;
            case [var name]:
                accounts.Selected.SelectedAgent = AssignAgent(accounts, name);
                break;
        }

        static AccountAgent AssignAgent(Account accounts, string name)
        {
            foreach (var a in accounts.Selected.Agents)
            {
                if (a.Name == name)
                    return a;
            }
            (Console.ForegroundColor, var fore) = (ConsoleColor.Red, Console.ForegroundColor);
            Console.WriteLine($"Unknown agent <{name}>");
            return accounts.Selected.SelectedAgent;
        }
    }

    void ParseShip(ReadOnlySpan<string> ship, ReadOnlySpan<string> previousCommands)
    {
        switch (ship)
        {
            case [] or ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [<shipName> | --help]");
                break;
            case [var name]:
                accounts.Selected.SelectedAgent.SelectedShip = AssignShip(accounts, name);
                break;
        }

        static V2.Ship AssignShip(Account accounts, string name)
        {
            if (accounts.Selected.SelectedAgent.Ships.TryGetValue(name, out var ship))
                return ship;
            (Console.ForegroundColor, var fore) = (ConsoleColor.Red, Console.ForegroundColor);
            Console.WriteLine($"Unknown ship <{name}>");
            return accounts.Selected.SelectedAgent.SelectedShip;
        }
    }
}

void ParseList(ReadOnlySpan<string> selection, ReadOnlySpan<string> previousCommands)
{
    switch (selection)
    {
        case [] or ["--help"]:
            Console.WriteLine($"{previousCommands.Concat()} [account | agent | ship | --help]");
            break;
        case ["account", .. var account]:
            ParseAccount(account, [..previousCommands, "account"]);
            break;
        case ["agent", .. var agent]:
            ParseAgent(agent, [..previousCommands, "agent"]);
            break;
        case ["ship", .. var ship]:
            ParseShip(ship, [..previousCommands, "ship"]);
            break;
    }

    void ParseAccount(ReadOnlySpan<string> account, ReadOnlySpan<string> previousCommands)
    {
        switch (account)
        {
            case ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [--help]");
                break;
            case []:
                foreach (var a in accounts.Accounts)
                    Console.WriteLine($"Account: {(a == accounts.Selected ? '*' : ' ')}{a.Name}");
                break;
        }
    }

    void ParseAgent(ReadOnlySpan<string> agent, ReadOnlySpan<string> previousCommands)
    {
        switch (agent)
        {
            case ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [--help]");
                break;
            case []:
                foreach (var a in accounts.Selected.Agents)
                    Console.WriteLine($"Agent: {(a == accounts.Selected.SelectedAgent ? '*' : ' ')}{a.Name}");
                break;
        }
    }

    void ParseShip(ReadOnlySpan<string> ship, ReadOnlySpan<string> previousCommands)
    {
        switch (ship)
        {
            case ["--help"]:
                Console.WriteLine($"{previousCommands.Concat()} [--help]");
                break;
            case []:
                foreach (var a in accounts.Selected.SelectedAgent.Ships)
                    Console.WriteLine($"Ship: {(a.Value == accounts.Selected.SelectedAgent.SelectedShip ? '*' : ' ')}{a.Key}");
                break;
        }
    }
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
            await foreach (var _ in agent.API.ListMyShips());
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

static class Ext
{
    extension(ReadOnlySpan<string> strings)
    {
        public string Concat()
        => string.Join(' ', strings);
    }

    extension(Console)
    {
        public static string Prompt(string prompt)
        {
            Console.Write(prompt);
            return Console.ReadLine()!;
        }
    }
}
