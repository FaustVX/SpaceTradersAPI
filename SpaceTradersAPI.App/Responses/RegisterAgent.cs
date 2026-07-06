using SpaceTradersAPI.App.Models.V2;

namespace SpaceTradersAPI.App.Responses;

public record class RegisterAgent(RegisterAgent.Datas Data)
{
    public record class Datas(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships);
}

