using System.Text.Json;
using SpaceTradersAPI.Lib;
using SpaceTradersAPI.Lib.Models;
using SpaceTradersAPI.Lib.Responses;

var accounts = await ReadAccounts(ReadFile());

while(true)
{
    switch (Console.Prompt("SpaceTrader>").Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        case [("sel" or "select") and var sel, .. var selection]:
            ParseSelect(selection, [sel]);
            break;
        case ["list", .. var selection]:
            await ParseList(selection, ["list"]);
            break;
        case ["info", .. var selection]:
            await ParseInfo(selection, ["info"]);
            break;
        case ["quit"]:
            Environment.Exit(0);
            break;
        case [] or ["--help"] or _:
            Console.WriteLine("[sel[ect] | list | info | quit | --help]");
            break;
    }
}

void ParseSelect(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["account", .. var account]:
            ParseAccount(account, [..previousCommands, "account"]);
            break;
        case ["agent", .. var agent]:
            ParseAgent(agent, [..previousCommands, "agent"]);
            break;
        case ["ship", .. var ship]:
            ParseShip(ship, [..previousCommands, "ship"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteLine($"{previousCommands.Concat()} [account | agent | ship | --help]");
            break;
    }

    void ParseAccount(string[] account, string[] previousCommands)
    {
        switch (account)
        {
            case ["--help"]:
                goto HELP;
            case [var name]:
                accounts.Selected = AssignAccount(accounts, name);
                break;
            case [] or _: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<accountName> | --help]");
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
            Console.ForegroundColor = fore;
            return accounts.Selected;
        }
    }

    void ParseAgent(string[] agent, string[] previousCommands)
    {
        switch (agent)
        {
            case ["--help"]:
                goto HELP;
            case [var name]:
                accounts.Selected.SelectedAgent = AssignAgent(accounts, name);
                break;
            case [] or _: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<agentName> | --help]");
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
            Console.ForegroundColor = fore;
            return accounts.Selected.SelectedAgent;
        }
    }

    void ParseShip(string[] ship, string[] previousCommands)
    {
        switch (ship)
        {
            case ["--help"]:
                goto HELP;
            case [var name]:
                accounts.Selected.SelectedAgent.SelectedShip = AssignShip(accounts, name);
                break;
            case [] or _: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<shipName> | --help]");
                break;
        }

        static V2.Ship AssignShip(Account accounts, string name)
        {
            if (accounts.Selected.SelectedAgent.Ships.TryGetValue(name, out var ship))
                return ship;
            (Console.ForegroundColor, var fore) = (ConsoleColor.Red, Console.ForegroundColor);
            Console.WriteLine($"Unknown ship <{name}>");
            Console.ForegroundColor = fore;
            return accounts.Selected.SelectedAgent.SelectedShip;
        }
    }
}

async Task ParseList(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["account", .. var account]:
            ParseAccount(account, [..previousCommands, "account"]);
            break;
        case ["agent", .. var agent]:
            await ParseAgent(agent, [..previousCommands, "agent"]);
            break;
        case ["ship", .. var ship]:
            ParseShip(ship, [..previousCommands, "ship"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteLine($"{previousCommands.Concat()} [account | agent | ship | --help]");
            break;
    }

    void ParseAccount(string[] account, string[] previousCommands)
    {
        switch (account)
        {
            case []:
                foreach (var a in accounts.Accounts)
                    Console.WriteLine($"Account: {(a == accounts.Selected ? '*' : ' ')}{a.Name}");
                break;
            case ["--help"] or _:
                Console.WriteLine($"{previousCommands.Concat()} [--help]");
                break;
        }
    }

    async Task ParseAgent(string[] agent, string[] previousCommands)
    {
        switch (agent)
        {
            case ["public"]:
                await foreach(var a in accounts.API.ListPublicAgents())
                    Console.WriteLine(a);
                    break;
            case []:
                foreach (var a in accounts.Selected.Agents)
                    Console.WriteLine($"Agent: {(a == accounts.Selected.SelectedAgent ? '*' : ' ')}{a.Name}");
                break;
            case ["--help"] or _:
                Console.WriteLine($"{previousCommands.Concat()} [public | --help]");
                break;
        }
    }

    void ParseShip(string[] ship, string[] previousCommands)
    {
        switch (ship)
        {
            case []:
                foreach (var a in accounts.Selected.SelectedAgent.Ships)
                    Console.WriteLine($"Ship: {(a.Value == accounts.Selected.SelectedAgent.SelectedShip ? '*' : ' ')}{a.Key}");
                break;
            case ["--help"] or _:
                Console.WriteLine($"{previousCommands.Concat()} [--help]");
                break;
        }
    }
}

async Task ParseInfo(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["ship", .. var ship]:
            await ParseShip(ship, [..previousCommands, "ship"]);
            break;
        case ["server"]:
            Console.WriteLine(await accounts.API.GetServerStatus());
            break;
        case [("way" or "waypoint") and var wp, .. var waypoint]:
            await ParseWaypoint(waypoint, [..previousCommands, wp]);
            break;
        case ["system", .. var system]:
            await ParseSystem(system, [..previousCommands, "system"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteLine($"{previousCommands.Concat()} [ship | server | way[point] | system | --help]");
            break;
    }

    async Task ParseShip(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case []:
                Console.WriteLine(await accounts.Selected.SelectedAgent.API.GetShip(accounts.Selected.SelectedAgent.SelectedShip.Symbol));
                break;
            case [var shipName]:
                Console.WriteLine(await accounts.Selected.SelectedAgent.API.GetShip(shipName));
                break;
            default: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<shipName> | --help]");
                break;
        }
    }

    async Task ParseWaypoint(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var waypoint] when V2.WaypointSymbol.TryParse(waypoint, default, out var wp):
                Console.WriteLine(await accounts.API.GetWaypoint(wp.InitWith(accounts)));
                break;
            case [] or _: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<waypoint> | --help]");
                break;
        }
    }

    async Task ParseSystem(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var system] when V2.SystemSymbol.TryParse(system, default, out var wp):
                Console.WriteLine(await accounts.API.GetSystem(wp.InitWith(accounts)));
                break;
            case [] or _: HELP:
                Console.WriteLine($"{previousCommands.Concat()} [<system> | --help]");
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
    extension(string[] strings)
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
