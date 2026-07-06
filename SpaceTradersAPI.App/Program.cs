using System.Net.Http.Headers;
using System.Text.Json;
using SpaceTradersAPI.App;

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
    return -1;
}

var accounts = ReadAccounts(accountsFile);
if (accounts.Selected.Agents is [])
{
    var data = JsonSerializer.Deserialize<SpaceTradersAPI.App.Responses.RegisterAgent>(File.OpenRead("test.json"), new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
    accounts.Selected.Agents.Add(new(data.Data.Agent.Symbol, data.Data.Token));
    File.WriteAllText(accountsFile.FullName, JsonSerializer.Serialize(accounts, JsonSerializerOptions.Default));
}

var request = new HttpRequestMessage(HttpMethod.Post, "/register")
{
    Headers =
    {
        Authorization = accounts.Selected.AccountToken,
    },
    Content = new StringContent("{\"symbol\": \"FAUST__VX\",\n  \"faction\": \"COSMIC\"}")
    {
        Headers =
        {
            ContentType = new MediaTypeHeaderValue("application/json")
        }
    }
};
using (var response = await accounts.HttpClient.SendAsync(request))
{
    response.EnsureSuccessStatusCode();
    var body = await response.Content.ReadFromJsonAsync<SpaceTradersAPI.App.Responses.RegisterAgent>()!;
}
return 0;

static Account ReadAccounts(FileInfo accountsFile)
{
    using var stream = accountsFile.OpenRead();
    return JsonSerializer.Deserialize<Account>(stream, new JsonSerializerOptions() { AllowTrailingCommas = true, PropertyNameCaseInsensitive = true, })!;
}
