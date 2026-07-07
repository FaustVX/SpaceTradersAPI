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

    public class Endpoints(Account account)
    {
        public Task<Responses.Agent[]> ListPublicAgents(int page = 1, int limit = 10)
        => account.SendAsyncData<Responses.Agent[]>(HttpMethod.Get, $"/agents?page={page}&limit={limit}");

        public Task<Responses.ServerStatus> GetServerStatus()
        => account.SendAsyncRaw<Responses.ServerStatus>(HttpMethod.Get, "/");
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
        public async Task<Responses.RegisterAgent> RegisterAgent(string symbol, string faction)
        {
            var registration = await account.Accounts.SendAsyncData<Responses.RegisterAgent>(HttpMethod.Post, "/register", account.AccountToken, $$"""{"symbol": "{{symbol}}",\n  "faction": "{{faction}}"}""");
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
        public Task<Responses.Agent> GetAgent()
        => agent.Accounts.SendAsyncData<Responses.Agent>(HttpMethod.Get, "/my/agent", agent.AgentToken);
    }
}
