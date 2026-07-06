using System.Net.Http.Headers;
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
}

public record class AccountItem(string Name, string Token)
{
    public List<AccountAgent> Agents { get; init; } = [];
    [JsonIgnore]
    public AuthenticationHeaderValue AccountToken => field ??= new("Bearer", Token);
    [JsonIgnore]
    public AccountAgent SelectedAgent { get => field ??= Agents[0]; set; }
}

public record class AccountAgent(string Name, string Token)
{
    [JsonIgnore]
    public AuthenticationHeaderValue AgentToken => field ??= new("Bearer", Token);
}
