using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTradersAPI.Lib;

public interface IAccount;

public record class Account(Uri BaseAddress, AccountItem[] Accounts) : IAccount
{
    private static readonly JsonSerializerOptions jsonSerializerOptions = new() { Converters = { new JsonStringEnumConverter() }, PropertyNameCaseInsensitive = true };

    [JsonIgnore]
    public AccountItem Selected { get => field ??= Accounts[0]; set; }
    [JsonIgnore]
    public HttpClient HttpClient => field ??= new()
    {
        BaseAddress = BaseAddress,
    };
    [JsonIgnore]
    public FileInfo File { get; set => field ??= value; } = default!;
    [JsonIgnore]
    public Endpoints API => field ??= new(this);

    public async Task<Responses.Result<T>> SendAsyncRaw<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    {
        var request = new HttpRequestMessage(method, endpoint)
        {
            Headers =
            {
                Authorization = token,
            },
            Content = content is null ? null : new StringContent(content)
            {
                Headers =
                {
                    ContentType = new MediaTypeHeaderValue("application/json")
                }
            }
        };
        using var response = await HttpClient.SendAsync(request);
        try
        {
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadFromJsonAsync<T>(jsonSerializerOptions))!;
        }
        catch (HttpRequestException ex) when (ex.StatusCode is System.Net.HttpStatusCode.TooManyRequests)
        {
            // var str = await response.Content.ReadAsStringAsync();
            var error = (await response.Content.ReadFromJsonAsync<Responses.ErrorResponse>(jsonSerializerOptions))!.Error;
            await Task.Delay(TimeSpan.Max(TimeSpan.Zero, JsonSerializer.Deserialize<DateTimeOffset>(error.Data!["reset"]) - DateTimeOffset.Now));
            return await SendAsyncRaw<T>(method, endpoint, token, content);
        }
        catch (HttpRequestException)
        {
            // var str = await response.Content.ReadAsStringAsync();
            return (await response.Content.ReadFromJsonAsync<Responses.ErrorResponse>(jsonSerializerOptions))!.Error;
        }
    }

    public Task<Responses.Result<T>> SendAsyncData<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    => SendAsyncRaw<Responses.Datas<T>>(method, endpoint, token, content).MapvalueAsync(data => data.Data);

    public async Task<Responses.Result<IAsyncEnumerable<T>>> SendAsyncEnumerable<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    {
        return await SendAsyncRaw<Responses.DatasWithMeta<T[]>>(method, endpoint + $"&limit=1", token, content) switch
        {
            Responses.Error err => err,
            _ => Enumerate(),
        };

        async IAsyncEnumerable<T> Enumerate()
        {
            endpoint += "&limit=20";
            for(var page = 1; true; page++)
            {
                var (data, _) = await SendAsyncRaw<Responses.DatasWithMeta<T[]>>(method, endpoint + $"&page={page}", token, content).ValueOrThrowAsync();
                foreach (var item in data)
                    yield return item;
                if (data.Length < 20)
                    yield break;
            }
        }
    }

    public class Endpoints(Account account)
    {
        public Task<Responses.Result<IAsyncEnumerable<Models.V2.Agent>>> ListPublicAgents()
        => account.SendAsyncEnumerable<Models.V2.Agent>(HttpMethod.Get, "/agents?")
        .MapInitAsync(account);

        public Task<Responses.Result<Models.V2.ServerStatus>> GetServerStatus()
        => account.SendAsyncRaw<Models.V2.ServerStatus>(HttpMethod.Get, "/");

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.Faction>>> ListFactions()
        => account.SendAsyncEnumerable<Models.V2.Faction>(HttpMethod.Get, "/factions?")
        .MapInitAsync(account);

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.Waypoint>>> ListWaypointInSystem(string systemSymbol, Models.V2.WaypointType? type = null, params Models.V2.WaypointTraitSymbol[] traits)
        => account.SendAsyncEnumerable<Models.V2.Waypoint>(HttpMethod.Get, $"/systems/{systemSymbol}/waypoints?{(type is Models.V2.WaypointType t ? $"type={t.ToUpperCase()}" : "")}{string.Concat(traits.Select(t => $"&traits={t.ToUpperCase()}"))}")
        .MapInitAsync(account);

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.Waypoint>>> ListWaypointInSystem(Models.V2.SystemSymbol systemSymbol, Models.V2.WaypointType? type = null, params Models.V2.WaypointTraitSymbol[] traits)
        => ListWaypointInSystem(systemSymbol.ToString(), type, traits);

        public Task<Responses.Result<Models.V2.Waypoint>> GetWaypoint(string systemSymbol, string waypointSymbol)
        => account.SendAsyncData<Models.V2.Waypoint>(HttpMethod.Get, $"/systems/{systemSymbol}/waypoints/{waypointSymbol}")
        .MapInitAsync(account);

        public Task<Responses.Result<Models.V2.Waypoint>> GetWaypoint(Models.V2.WaypointSymbol waypointSymbol)
        => GetWaypoint(waypointSymbol.System.ToString(), waypointSymbol.ToString());

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.System>>> ListSystems()
        => account.SendAsyncEnumerable<Models.V2.System>(HttpMethod.Get, "/systems?")
        .MapInitAsync(account);

        public Task<Responses.Result<Models.V2.System>> GetSystem(string systemSymbol)
        => account.SendAsyncData<Models.V2.System>(HttpMethod.Get, $"/systems/{systemSymbol}")
        .MapInitAsync(account);

        public Task<Responses.Result<Models.V2.System>> GetSystem(Models.V2.SystemSymbol systemSymbol)
        => GetSystem(systemSymbol.ToString());
    }
}

public record class AccountItem(string Name, string Token) : IAccount
{
    public List<AccountAgent> Agents { get; init; } = [];
    [JsonIgnore]
    public AuthenticationHeaderValue AccountToken => field ??= new("Bearer", Token);
    [JsonIgnore]
    public AccountAgent SelectedAgent { get => field ??= Agents[0]; set; }
    [JsonIgnore]
    public Account Accounts { get; set => field ??= value; } = default!;
    [JsonIgnore]
    public Endpoints API => field ??= new(this);

    public class Endpoints(AccountItem account)
    {
        public Task<Responses.Result<Models.V2.RegisterAgent>> RegisterAgent(string symbol, Models.V2.FactionSymbol faction)
        => account.Accounts.SendAsyncData<Models.V2.RegisterAgent>(HttpMethod.Post, "/register", account.AccountToken, $$"""{"symbol":"{{symbol}}","faction":"{{faction.ToUpperCase()}}"}""")
            .MapvalueAsync(registration =>
            {
                var accountAgent = new AccountAgent(registration.Agent.Symbol, registration.Token) { Account = account };
                account.Agents.Add(accountAgent);
                account.SelectedAgent = accountAgent;
                File.WriteAllText(account.Accounts.File.FullName, JsonSerializer.Serialize(account.Accounts, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true, }));
                return registration.InitWith(accountAgent);
            });
    }
}

public record class AccountAgent(string Name, string Token) : IAccount
{
    [JsonIgnore]
    public AuthenticationHeaderValue AgentToken => field ??= new("Bearer", Token);
    [JsonIgnore]
    public AccountItem Account { get; set; } = default!;
    [JsonIgnore]
    public Dictionary<string, Models.V2.Ship> Ships { get; } = [];
    [JsonIgnore]
    public Models.V2.Ship SelectedShip { get => field ??= Ships.Values.FirstOrDefault()!; set; }
    [JsonIgnore]
    public Endpoints API => field ??= new(this);

    public class Endpoints(AccountAgent agent)
    {
        public Task<Responses.Result<Models.V2.Agent>> GetAgent()
        => agent.Account.Accounts.SendAsyncData<Models.V2.Agent>(HttpMethod.Get, "/my/agent", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.AgentFaction>>> GetMyFactions()
        => agent.Account.Accounts.SendAsyncEnumerable<Models.V2.AgentFaction>(HttpMethod.Get, "/my/factions?", agent.AgentToken);

        public async Task<Responses.Result<Models.V2.Ship>> GetShip(string shipSymbol)
        {
            switch (await agent.Account.Accounts.SendAsyncData<Models.V2.Ship>(HttpMethod.Get, $"/my/ships/{shipSymbol}", agent.AgentToken)
                .MapInitAsync(agent))
            {
                case Models.V2.Ship ship:
                    agent.Ships[ship.Symbol] = ship;
                    if (agent.SelectedShip.Symbol == ship.Symbol)
                        agent.SelectedShip = ship;
                    return ship;
                case var result:
                    return result;
            }
        }

        public Task<Responses.Result<Models.V2.Contract>> NegociateContract(string shipSymbol)
        => agent.Account.Accounts.SendAsyncData<Models.V2.Contract>(HttpMethod.Post, $"/my/ships/{shipSymbol}/negotiate/contract", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.ShipNav>> DockShip(string shipSymbol)
        => agent.Account.Accounts.SendAsyncData<Responses.ShipNavWraper>(HttpMethod.Post, $"/my/ships/{shipSymbol}/dock", agent.AgentToken)
        .MapvalueAsync(resp => resp.Nav).MapInitAsync(agent.Account.Accounts);

        public Task<Responses.Result<Models.V2.ShipNav>> OrbitShip(string shipSymbol)
        => agent.Account.Accounts.SendAsyncData<Responses.ShipNavWraper>(HttpMethod.Post, $"/my/ships/{shipSymbol}/orbit", agent.AgentToken)
        .MapvalueAsync(resp => resp.Nav).MapInitAsync(agent.Account.Accounts);

        public Task<Responses.Result<Models.V2.CreateChart>> CreateChart(string shipSymbol)
        => agent.Account.Accounts.SendAsyncData<Models.V2.CreateChart>(HttpMethod.Post, $"/my/ships/{shipSymbol}/chart", agent.AgentToken)
        .MapInitAsync(agent);

        public async Task<Responses.Result<IAsyncEnumerable<Models.V2.Ship>>> ListMyShips()
        {
            return await agent.Account.Accounts.SendAsyncEnumerable<Models.V2.Ship>(HttpMethod.Get, "/my/ships?", agent.AgentToken)
                .MapInitAsync(agent) switch
            {
                Responses.Error err => err,
                IAsyncEnumerable<Models.V2.Ship> ships => Enumerate(ships),
                _ => throw new UnreachableException(),
            };

            async IAsyncEnumerable<Models.V2.Ship> Enumerate(IAsyncEnumerable<Models.V2.Ship> ships)
            {
                await foreach (var ship in ships)
                {
                    agent.Ships[ship.Symbol] = ship;
                    if (agent.SelectedShip.Symbol == ship.Symbol)
                        agent.SelectedShip = ship;
                    yield return ship;
                }
            }
        }

        public Task<Responses.Result<IAsyncEnumerable<Models.V2.Contract>>> ListMyContracts()
        => agent.Account.Accounts.SendAsyncEnumerable<Models.V2.Contract>(HttpMethod.Get, "/my/contracts?", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.Contract>> GetContract(string contractId)
        => agent.Account.Accounts.SendAsyncData<Models.V2.Contract>(HttpMethod.Get, $"/my/contracts/{contractId}", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.AcceptContract>> AcceptContract(string contractId)
        => agent.Account.Accounts.SendAsyncData<Models.V2.AcceptContract>(HttpMethod.Post, $"/my/contracts/{contractId}/accept", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.AcceptContract>> FulfillContract(string contractId)
        => agent.Account.Accounts.SendAsyncData<Models.V2.AcceptContract>(HttpMethod.Post, $"/my/contracts/{contractId}/fulfull", agent.AgentToken)
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.DeliverCargoToContract>> DeliverCargoToContract(string contractId, string shipSymbol, string tradeSymbol, int units)
        => agent.Account.Accounts.SendAsyncData<Models.V2.DeliverCargoToContract>(HttpMethod.Post, $"/my/contracts/{contractId}/deliver", agent.AgentToken, $$"""{"shipSymbol":"{{shipSymbol}}","tradeSymbol":"{{tradeSymbol}}","units":{{units}}}""")
        .MapInitAsync(agent);

        public Task<Responses.Result<Models.V2.NavigateShip>> NavigateShip(string shipSymbol, string waypointSymbol)
        => agent.Account.Accounts.SendAsyncData<Models.V2.NavigateShip>(HttpMethod.Post, $"/my/ships/{shipSymbol}/navigate", agent.AgentToken, $$"""{"waypointSymbol":"{{waypointSymbol}}"}""")
        .MapInitAsync(agent.Account.Accounts);

        public Task<Responses.Result<Models.V2.ShipNav>> GetShipNav(string shipSymbol)
        => agent.Account.Accounts.SendAsyncData<Models.V2.ShipNav>(HttpMethod.Get, $"/my/ships/{shipSymbol}/nav", agent.AgentToken)
        .MapInitAsync(agent.Account.Accounts);

        public Task<Responses.Result<Models.V2.NavigateShip>> PatchShipNav(string shipSymbol, string flightMode)
        => agent.Account.Accounts.SendAsyncData<Models.V2.NavigateShip>(HttpMethod.Patch, $"/my/ships/{shipSymbol}/nav", agent.AgentToken, $$"""{"flightMode":"{{flightMode}}"}""")
        .MapInitAsync(agent.Account.Accounts);

        public Task<Responses.Result<Models.V2.RefuelShip>> RefuelShip(string shipSymbol, int? units = null, bool? fromCargo = null)
        => agent.Account.Accounts.SendAsyncData<Models.V2.RefuelShip>(HttpMethod.Patch, $"/my/ships/{shipSymbol}/refuel", agent.AgentToken, (units, fromCargo) switch
        {
            (null, null) => "{}",
            (int u, bool c) => $$"""{"units":"{{u}}","fromCargo":"{{(c ? "true" : "false")}}"}""",
            (int u, null) => $$"""{"units":"{{u}}"}""",
            (null, bool c) => $$"""{"fromCargo":"{{(c ? "true" : "false")}}"}""",
        })
        .MapInitAsync(agent);
    }
}
