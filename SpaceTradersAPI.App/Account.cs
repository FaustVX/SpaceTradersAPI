using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTradersAPI.App;

public record class Account(Uri BaseAddress, AccountItem[] Accounts)
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
        catch (HttpRequestException)
        {
            return (await response.Content.ReadFromJsonAsync<Responses.ErrorResponse>(jsonSerializerOptions))!.Error;
        }
    }

    public Task<Responses.Result<T>> SendAsyncData<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    => SendAsyncRaw<Responses.Datas<T>>(method, endpoint, token, content).MapvalueAsync(data => data.Data);

    public async IAsyncEnumerable<T> SendAsyncEnumerable<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    {
        endpoint += "?limit=20";
        for(var page = 1; true; page++)
        {
            var (data, _) = await SendAsyncRaw<Responses.DatasWithMeta<T[]>>(method, endpoint + $"&page={page}", token, content).ValueOrThrowAsync();
            foreach (var item in data)
                yield return item;
            if (data.Length < 20)
                yield break;
        }
    }

    public class Endpoints(Account account)
    {
        public IAsyncEnumerable<Models.V2.Agent> ListPublicAgents()
        => account.SendAsyncEnumerable<Models.V2.Agent>(HttpMethod.Get, "/agents");

        public Task<Responses.Result<Models.V2.ServerStatus>> GetServerStatus()
        => account.SendAsyncRaw<Models.V2.ServerStatus>(HttpMethod.Get, "/");

        public IAsyncEnumerable<Models.V2.Faction> ListFactions()
        => account.SendAsyncEnumerable<Models.V2.Faction>(HttpMethod.Get, "/factions");
    }
}

public record class AccountItem(string Name, string Token)
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
                var accountAgent = new AccountAgent(registration.Agent.Symbol, registration.Token) { Accounts = account.Accounts };
                account.Agents.Add(accountAgent);
                foreach (var ship in registration.Ships)
                    ship.AccountAgent = accountAgent;
                registration.Agent.AccountAgent = accountAgent;
                File.WriteAllText(account.Accounts.File.FullName, JsonSerializer.Serialize(account.Accounts, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true, }));
                return registration;
            });

        public IAsyncEnumerable<Models.V2.AgentFaction> GetMyFactions()
        => account.Accounts.SendAsyncEnumerable<Models.V2.AgentFaction>(HttpMethod.Get, "/my/factions", account.AccountToken);
    }
}

public record class AccountAgent(string Name, string Token)
{
    [JsonIgnore]
    public AuthenticationHeaderValue AgentToken => field ??= new("Bearer", Token);
    [JsonIgnore]
    public Account Accounts { get; set; } = default!;
    [JsonIgnore]
    public AccountItem Account { get; set; } = default!;
    [JsonIgnore]
    public Endpoints API => field ??= new(this);

    public class Endpoints(AccountAgent agent)
    {
        public Task<Responses.Result<Models.V2.Agent>> GetAgent()
        => agent.Accounts.SendAsyncData<Models.V2.Agent>(HttpMethod.Get, "/my/agent", agent.AgentToken)
        .MapvalueAsync(a => a with { AccountAgent = agent });

        public Task<Responses.Result<Models.V2.Ship>> GetShip(string shipSymbol)
        => agent.Accounts.SendAsyncData<Models.V2.Ship>(HttpMethod.Get, $"/my/ships/{shipSymbol}", agent.AgentToken)
        .MapvalueAsync(resp => resp with { AccountAgent = agent });

        public Task<Responses.Result<Models.V2.ShipNav>> DockShip(string shipSymbol)
        => agent.Accounts.SendAsyncData<Responses.ShipNavWraper>(HttpMethod.Post, $"/my/ships/{shipSymbol}/dock", agent.AgentToken)
        .MapvalueAsync(resp => resp.Nav);

        public Task<Responses.Result<Models.V2.ShipNav>> OrbitShip(string shipSymbol)
        => agent.Accounts.SendAsyncData<Responses.ShipNavWraper>(HttpMethod.Post, $"/my/ships/{shipSymbol}/orbit", agent.AgentToken)
        .MapvalueAsync(resp => resp.Nav);

        public Task<Responses.Result<Models.V2.CreateChart>> CreateChart(string shipSymbol)
        => agent.Accounts.SendAsyncData<Models.V2.CreateChart>(HttpMethod.Post, $"/my/ships/{shipSymbol}/chart", agent.AgentToken)
        .MapvalueAsync(chart => chart with { Agent = chart.Agent with { AccountAgent = agent } });

        public async IAsyncEnumerable<Models.V2.Ship> ListMyShips()
        {
            await foreach (var ship in agent.Accounts.SendAsyncEnumerable<Models.V2.Ship>(HttpMethod.Get, "/my/ships", agent.AgentToken))
            {
                ship.AccountAgent = agent;
                yield return ship;
            }
        }

        public IAsyncEnumerable<Models.V2.Contract> ListMyContracts()
        => agent.Accounts.SendAsyncEnumerable<Models.V2.Contract>(HttpMethod.Get, "/my/contracts");
    }
}
