using SpaceTradersAPI.App.Models.V2;

namespace SpaceTradersAPI.App.Responses;

public record class Datas<T>(T Data);

public record class DatasWithMeta<T>(T Data, Meta Meta);

public record class Meta(int Total, int Page, int Limit);

public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships);

public record class Agent(string AccountID, string Symbol, string HeadQuarters, long Credits, string StartingFaction, int ShipCount);

public record class AgentFaction(string Symbol, int Reputation);

public record class ServerStatus(string Status, string Version, DateOnly ResetDate, ServerStatusHealth Health, ServerStatusResets ServerResets);

public record class ServerStatusHealth(DateTimeOffset LastMarketUpdate);

public record class ServerStatusResets(DateTimeOffset Next, string Frequency);
