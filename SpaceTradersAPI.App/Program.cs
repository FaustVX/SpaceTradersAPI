using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using SpaceTradersAPI.Lib;
using SpaceTradersAPI.Lib.Models;
using SpaceTradersAPI.Lib.Responses;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

var accounts = await ReadAccounts(ReadFile());

while(!Console.IsInputRedirected || Console.In.Peek() is not -1)
    try
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
            case ["register", .. var selection]:
                await ParseRegister(selection, ["register"]);
                break;
            case ["contract", .. var selection]:
                await ParseContract(selection, ["contract"]);
                break;
            case ["ship", .. var selection]:
                await ParseShip(selection, ["ship"]);
                break;
            case ["quit"]:
                Environment.Exit(0);
                break;
            case [] or ["--help"] or _:
                Console.WriteInfo("[sel[ect] | list | info | register | contract | ship | quit | --help]");
                break;
        }
    }
    catch (Exception ex)
    {
        Console.WriteError(ex.Message);
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
            Console.WriteInfo($"{previousCommands.Concat()} [account | agent | ship | --help]");
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
                Console.WriteInfo($"{previousCommands.Concat()} [<accountName> | --help]");
                break;
        }

        static AccountItem AssignAccount(Account accounts, string name)
        {
            foreach (var a in accounts.Accounts)
            {
                if (a.Name == name)
                    return a;
            }
            Console.WriteError($"Unknown account <{name}>");
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
                Console.WriteInfo($"{previousCommands.Concat()} [<agentName> | --help]");
                break;
        }

        static AccountAgent AssignAgent(Account accounts, string name)
        {
            foreach (var a in accounts.Selected.Agents)
            {
                if (a.Name == name)
                    return a;
            }
            Console.WriteError($"Unknown agent <{name}>");
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
                Console.WriteInfo($"{previousCommands.Concat()} [<shipName> | --help]");
                break;
        }

        static V2.Ship AssignShip(Account accounts, string name)
        {
            if (accounts.Selected.SelectedAgent.Ships.TryGetValue(name, out var ship))
                return ship;
            Console.WriteError($"Unknown ship <{name}>");
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
        case ["factions", .. var factions]:
            await ParseFactions(factions, [..previousCommands, "factions"]);
            break;
        case ["systems"]:
            await accounts.API.ListSystems().Execute(Console.WriteValue, Console.WriteError);
            break;
        case [("way" or "waypoint") and var wp, .. var waypoint]:
            await ParseWaypointsInSystem(waypoint, [..previousCommands, wp]);
            break;
        case ["contracts"]:
            await accounts.Selected.SelectedAgent.API.ListMyContracts().Execute(c => Console.WriteLine($"Agent: {(c.Id == accounts.Selected.SelectedAgent.SelectedContract?.Id ? '*' : ' ')}{c}"), Console.WriteError);
            break;
        case [] or ["--help"] or _:
            Console.WriteInfo($"{previousCommands.Concat()} [account | agent | ship | factions | systems | way[point] | contracts | --help]");
            break;
    }

    void ParseAccount(string[] account, string[] previousCommands)
    {
        switch (account)
        {
            case []:
                foreach (var a in accounts.Accounts)
                    Console.WriteLine($"Account: {(a.Name == accounts.Selected?.Name ? '*' : ' ')}{a.Name}");
                break;
            case ["--help"] or _:
                Console.WriteInfo($"{previousCommands.Concat()} [--help]");
                break;
        }
    }

    async Task ParseAgent(string[] agent, string[] previousCommands)
    {
        switch (agent)
        {
            case ["public"]:
                await accounts.API.ListPublicAgents().Execute(Console.WriteValue, Console.WriteError);
                break;
            case []:
                foreach (var a in accounts.Selected.Agents)
                    Console.WriteLine($"Agent: {(a.Name == accounts.Selected.SelectedAgent?.Name ? '*' : ' ')}{a.Name}");
                break;
            case ["--help"] or _:
                Console.WriteInfo($"{previousCommands.Concat()} [public | --help]");
                break;
        }
    }

    void ParseShip(string[] ship, string[] previousCommands)
    {
        switch (ship)
        {
            case []:
                foreach (var a in accounts.Selected.SelectedAgent.Ships)
                    Console.WriteLine($"Ship: {(a.Key == accounts.Selected.SelectedAgent.SelectedShip?.Symbol ? '*' : ' ')}{a.Key}");
                break;
            case ["--help"] or _:
                Console.WriteInfo($"{previousCommands.Concat()} [--help]");
                break;
        }
    }

    async Task ParseFactions(string[] factions, string[] previousCommands)
    {
        switch (factions)
        {
            case ["public"]:
                await accounts.API.ListFactions().Execute(Console.WriteValue, Console.WriteError);
                break;
            case []:
                await accounts.Selected.SelectedAgent.API.GetMyFactions().Execute(Console.WriteValue, Console.WriteError);
                break;
            case ["--help"] or _:
                Console.WriteInfo($"{previousCommands.Concat()} [public | --help]");
                break;
        }
    }

    async Task ParseWaypointsInSystem(string[] waypoint, string[] previousCommands)
    {
        switch (waypoint)
        {
            case ["--help"]:
                goto HELP;
            case [var system] when V2.SystemSymbol.TryParse(system, default, out var sys):
                await accounts.API.ListWaypointInSystem(sys.InitWith(accounts)).Execute(Console.WriteValue, Console.WriteError);
                break;
            case [var system, var type] when V2.SystemSymbol.TryParse(system, default, out var sys) && Enum.TryParse<V2.WaypointType>(type, out var ty):
                await accounts.API.ListWaypointInSystem(sys.InitWith(accounts), ty).Execute(Console.WriteValue, Console.WriteError);
                break;
            case [var system, var type, .. var traits] when V2.SystemSymbol.TryParse(system, default, out var sys) && Enum.TryParse<V2.WaypointType>(type, out var ty) && TryParseTraits(traits, out var tr):
                await accounts.API.ListWaypointInSystem(sys.InitWith(accounts), ty, tr).Execute(Console.WriteValue, Console.WriteError);
                break;
            case [var system, .. var traits] when V2.SystemSymbol.TryParse(system, default, out var sys) && TryParseTraits(traits, out var tr):
                await accounts.API.ListWaypointInSystem(sys.InitWith(accounts), traits: tr).Execute(Console.WriteValue, Console.WriteError);
                break;
            case [] or ["--help"] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<system> [<type>] [<trailt...>]| --help]");
                break;
        }

        static bool TryParseTraits(string[] input, [NotNullWhen(true)]out V2.WaypointTraitSymbol[]? traits)
        {
            try
            {
                traits = [.. input.Select(Enum.Parse<V2.WaypointTraitSymbol>)];
                return true;
            }
            catch
            {
                traits = default;
                return false;
            }
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
            Console.WriteValue(await accounts.API.GetServerStatus());
            break;
        case [("way" or "waypoint") and var wp, .. var waypoint]:
            await ParseWaypoint(waypoint, [..previousCommands, wp]);
            break;
        case ["system", .. var system]:
            await ParseSystem(system, [..previousCommands, "system"]);
            break;
        case ["agent"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.API.GetAgent());
            break;
        case ["contract", .. var contract]:
        await ParseContract(contract, [..previousCommands, "contract"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteInfo($"{previousCommands.Concat()} [ship | server | way[point] | system | agent | contract | --help]");
            break;
    }

    async Task ParseShip(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case []:
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.UpdateFromServer());
                break;
            case [var shipName]:
                Console.WriteValue(await accounts.Selected.SelectedAgent.API.GetShip(shipName));
                break;
            default: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<shipName> | --help]");
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
                Console.WriteValue(await wp.InitWith(accounts).GetWaypointData());
                break;
            case [] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<waypoint> | --help]");
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
                Console.WriteValue(await accounts.API.GetSystem(wp.InitWith(accounts)));
                break;
            case [] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<system> | --help]");
                break;
        }
    }

    async Task ParseContract(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var id]:
                Console.WriteValue(await accounts.Selected.SelectedAgent.API.GetContract(id));
                break;
            case [] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<contractId> | --help]");
                break;
        }
    }
}

async Task ParseRegister(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["agent", var name, var faction] when Enum.TryParse<V2.FactionSymbol>(faction, out var fac):
            Console.WriteValue(await accounts.Selected.API.RegisterAgent(name, fac));
            break;
        case [] or ["--help"] or _:
            Console.WriteInfo($"{previousCommands.Concat()} [agent <name> <faction> | --help]");
            break;
    }
}

async Task ParseContract(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["negociate"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.NegociateContract());
            break;
        case ["accept", .. var accept]:
            await ParseAccept(accept, [..previousCommands, "accept"], accounts.Selected.SelectedAgent.API.AcceptContract);
            break;
        case ["fulfill", .. var accept]:
            await ParseAccept(accept, [..previousCommands, "fulfill"], accounts.Selected.SelectedAgent.API.FulfillContract);
            break;
        case ["info", .. var accept]:
            await ParseAccept(accept, [..previousCommands, "info"], accounts.Selected.SelectedAgent.API.GetContract);
            break;
        case ["deliver", .. var accept]:
            await ParseDeliver(accept, [..previousCommands, "deliver"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteInfo($"{previousCommands.Concat()} [negociate | accept | fulfill | info | deliver | --help]");
            break;
    }

    async Task ParseAccept<T>(string[] selection, string[] previousCommands, Func<string, Task<Result<T>>> func)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var id]:
                Console.WriteValue(await func(id));
                break;
            case [] when accounts.Selected.SelectedAgent.SelectedContract is { Id: var id }:
                Console.WriteValue(await func(id));
                break;
            case [] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<contractId> | --help]");
                break;
        }
    }

    async Task ParseDeliver(string[] selection, string[] previousCommands)
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var id, var trade, var units] when Enum.TryParse<V2.TradeSymbol>(trade, out var tr) && int.TryParse(units, out var un):
                var contract = accounts.Selected.SelectedAgent.SelectedContract!;
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.DeliverCargoToContract(contract, tr, un));
                break;
            case [var trade, var units] when accounts.Selected.SelectedAgent.SelectedContract is {} c && Enum.TryParse<V2.TradeSymbol>(trade, out var tr) && int.TryParse(units, out var un):
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.DeliverCargoToContract(c, tr, un));
                break;
            case [] or _: HELP:
                Console.WriteInfo($"{previousCommands.Concat()} [<contractId> <trade> <units> | --help]");
                break;
        }
    }
}

async Task ParseShip(string[] selection, string[] previousCommands)
{
    switch (selection)
    {
        case ["dock"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Dock());
            break;
        case ["orbit"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Orbit());
            break;
        case ["chart"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.CreateChart());
            break;
        case ["info"]:
            Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.UpdateFromServer());
            break;
        case [("nav" or "navigate") and var navigate, .. var nav]:
            await ParseNavigate(nav, [..previousCommands, navigate]);
            break;
        case ["refuel", .. var nav]:
            await ParseRefuel(nav, [..previousCommands, "refuel"]);
            break;
        case [] or ["--help"] or _:
            Console.WriteInfo($"{previousCommands.Concat()} [dock | orbit | chart | info | nav[igate] | refuel | --help]");
            break;
    }

    async Task ParseNavigate(string[] selection, string[] previousCommands) 
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case [var waypoint] when V2.WaypointSymbol.TryParse(waypoint, default, out var wp):
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Navigate(wp));
                break;
            case [var local]:
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Navigate(accounts.Selected.SelectedAgent.SelectedShip.Nav.WaypointSymbol.InitWith(accounts) with { Waypoint = local }));
                break;
        case [] or _: HELP:
            Console.WriteInfo($"{previousCommands.Concat()} [<waypoint> | <localWaypoint> | --help]");
            break;
        }
    }

    async Task ParseRefuel(string[] selection, string[] previousCommands) 
    {
        switch (selection)
        {
            case ["--help"]:
                goto HELP;
            case []:
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Refuel());
                break;
            case ["fromCargo"]:
                if (accounts.Selected.SelectedAgent.SelectedShip.Cargo.Get(V2.TradeSymbol.Fuel) is {} item0)
                    Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Refuel(item0));
                else
                    Console.WriteError("Cargo does not contains enough Fuel");
                break;
            case [var units] when int.TryParse(units, out var un):
                Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Refuel(un));
                break;
            case [var units, "fromCargo"] when int.TryParse(units, out var un):
                if (accounts.Selected.SelectedAgent.SelectedShip.Cargo.Get(V2.TradeSymbol.Fuel, un) is {} item1)
                    Console.WriteValue(await accounts.Selected.SelectedAgent.SelectedShip.Refuel(item1));
                else
                    Console.WriteError("Cargo does not contains enough Fuel");
                break;
        case [] or _: HELP:
            Console.WriteInfo($"{previousCommands.Concat()} [[<units>] [fromCargo] | --help]");
            break;
        }
    }
}

static async Task<Account> ReadAccounts(FileInfo accountsFile)
{
    using var stream = accountsFile.OpenRead();
    var accounts = JsonSerializer.Deserialize<Account>(stream, new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
    var status = await accounts.API.GetServerStatus().ValueOrThrowAsync();
    Console.WriteValue(status);
    if (status.Version != "v2.3.0")
        throw new Exception($"Incompatible server version. Should be v2.3.0 but server is {status.Version}");
    foreach (var account in accounts.Accounts)
    {
        account.Accounts = accounts;
        foreach (var agent in account.Agents)
        {
            agent.Account = account;
            await foreach (var _ in agent.API.ListMyShips());
            await foreach (var _ in agent.API.ListMyContracts());
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
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new() { Converters = { new JsonStringEnumConverter() } };

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

        public static void WriteValue<T>(Result<T> value)
        {
            switch (value)
            {
                case T item:
                    Console.WriteValue(item);
                    break;
                case Error err:
                    Console.WriteError(err);
                    break;
            }
        }

        public static void WriteValue(string value)
        => Console.WriteLine(value);

        public static void WriteValue<T>(T value)
        {
            switch (value)
            {
                case string s:
                    Console.WriteValue(s);
                    break;
                default:
                    AnsiConsole.WriteLine(new JsonText(JsonSerializer.Serialize(value, _jsonSerializerOptions)) { Indentation = "  " });
                    break;
            };
        }

        public static void WriteError(string value)
        {
            (Console.ForegroundColor, var fore) = (ConsoleColor.Red, Console.ForegroundColor);
            Console.WriteLine(value);
            Console.ForegroundColor = fore;
        }

        public static void WriteError<T>(T value)
        where T : notnull
        {
            switch (value)
            {
                case string s:
                    Console.WriteError(s);
                    break;
                default:
                    AnsiConsole.WriteLine(new JsonText(value.GetType() == typeof(T) ? JsonSerializer.Serialize(value, _jsonSerializerOptions) : JsonSerializer.Serialize(value, value.GetType(), _jsonSerializerOptions)) { MemberStyle = Color.Orange1, Indentation = "  " });
                    break;
            }
        }

        public static void WriteInfo(string value)
        {
            (Console.ForegroundColor, var fore) = (ConsoleColor.Blue, Console.ForegroundColor);
            Console.WriteLine(value);
            Console.ForegroundColor = fore;
        }
    }

    extension(AnsiConsole)
    {
        public static void WriteLine(IRenderable renderable)
        {
            AnsiConsole.Write(renderable);
            AnsiConsole.WriteLine();
        }
    }
}
