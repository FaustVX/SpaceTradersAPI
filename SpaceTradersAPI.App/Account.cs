using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTradersAPI.App;

public record class Account(Uri BaseAddress, AccountItem[] Accounts)
{
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

    public async Task<T> SendAsyncRaw<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
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
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public async Task<T> SendAsyncData<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    => (await SendAsyncRaw<Responses.Datas<T>>(method, endpoint, token, content)).Data;

    public async IAsyncEnumerable<T> SendAsyncEnumerable<T>(HttpMethod method, string endpoint, AuthenticationHeaderValue? token = null, string? content = null)
    {
        endpoint += "?limit=20";
        for(var page = 1; true; page++)
        {
            var (data, _) = await SendAsyncRaw<Responses.DatasWithMeta<T[]>>(method, endpoint + $"&page={page}", token, content);
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

        public Task<Models.V2.ServerStatus> GetServerStatus()
        => account.SendAsyncRaw<Models.V2.ServerStatus>(HttpMethod.Get, "/");
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
        public async Task<Models.V2.RegisterAgent> RegisterAgent(string symbol, string faction)
        {
            var registration = await account.Accounts.SendAsyncData<Models.V2.RegisterAgent>(HttpMethod.Post, "/register", account.AccountToken, $$"""{"symbol": "{{symbol}}",\n  "faction": "{{faction}}"}""");
            account.Agents.Add(new(registration.Agent.Symbol, registration.Token) { Accounts = account.Accounts });
            File.WriteAllText(account.Accounts.File.FullName, JsonSerializer.Serialize(account.Accounts, new JsonSerializerOptions(JsonSerializerDefaults.General) { WriteIndented = true, }));
            return registration;
        }
    }
}

public record class AccountAgent(string Name, string Token)
{
    [JsonIgnore]
    public AuthenticationHeaderValue AgentToken => field ??= new("Bearer", Token);
    [JsonIgnore]
    public Account Accounts { get; set; } = default!;
    [JsonIgnore]
    public Endpoints API => field ??= new(this);

    public class Endpoints(AccountAgent agent)
    {
        public Task<Models.V2.Agent> GetAgent()
        => agent.Accounts.SendAsyncData<Models.V2.Agent>(HttpMethod.Get, "/my/agent", agent.AgentToken);

        public IAsyncEnumerable<Models.V2.Ship> ListMyShips()
        => agent.Accounts.SendAsyncEnumerable<Models.V2.Ship>(HttpMethod.Get, "/my/ships", agent.AgentToken);
    }
}
