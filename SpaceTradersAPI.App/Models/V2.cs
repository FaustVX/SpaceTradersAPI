using System.Text.Json.Serialization;

namespace SpaceTradersAPI.App.Models;

public static class V2
{
    public record class Agent(string AccountID, string Symbol, string HeadQuarters, long Credits, string StartingFaction, int ShipCount)
    {
        private AccountItem _account = default!;

        [JsonIgnore]
        public AccountItem AccountAgent { set => _account ??= value; }

        public Task<Responses.Result<RegisterAgent>> RegisterAgent(string symbol, string faction)
        => _account.API.RegisterAgent(symbol, faction);
    }

    public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships);

    public record class AgentFaction(string Symbol, int Reputation);

    public record class ServerStatus(string Status, string Version, DateOnly ResetDate, ServerStatusHealth Health, ServerStatusResets ServerResets);

    public record class ServerStatusHealth(DateTimeOffset LastMarketUpdate);

    public record class ServerStatusResets(DateTimeOffset Next, string Frequency);

    public record class Faction(string Symbol, string Name, string Description, string HeadQuarters, FactionTrait[] Traits, bool IsRecruiting);

    public record class FactionTrait(string Symbol, string Name, string Description);

    public record class Contract(string Id, string FactionSymbol, string Type, ContractTerm Terms, bool Accepted, bool FulFilled, DateTimeOffset DeadlineToAccept);

    public record class ContractTerm(DateTimeOffset Deadline, ContractPayment Payment, ContractDeliverGood[] Deliver);

    public record class ContractPayment(int OnAccepted, int OnFulfilled);

    public record class ContractDeliverGood(string TradeSymbol, string DestinationSymbol, int UnitsRequired, int UnitsFulFilled);

    public record class Ship(string Symbol, ShipRegistration Registration, ShipNav Nav, ShipCrew Crew, ShipFrame Frame, ShipReactor Reactor, ShipEngine Engine, ShipModule[] Modules, ShipMount[] Mounts, ShipCargo Cargo, ShipFuel Fuel, ShipCooldown Cooldown)
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }
        public Task<Responses.Result<ShipNav>> Dock()
        => _accountAgent.API.DockShip(Symbol);

        public Task<Responses.Result<ShipNav>> Orbit()
        => _accountAgent.API.OrbitShip(Symbol);

        public Task<Responses.Result<CreateChart>> CreateChart()
        => _accountAgent.API.CreateChart(Symbol);

        public Task<Responses.Result<Ship>> UpdateFromServer()
        => _accountAgent.API.GetShip(Symbol);
    }

    public record class ShipRegistration(string Name, string FactionSymbol, string Role);

    public record class ShipNav(string SystemSymbol, string WaypointSymbol, ShipNavRoute Route, string Status, string FlightMode);

    public record class ShipNavRoute(ShipNavRouteWaypoint Destination, ShipNavRouteWaypoint Origin, DateTimeOffset DepartureTime, DateTimeOffset Arrival);

    public record class ShipNavRouteWaypoint(string Symbol, string Type, string SystemSymbol, int X, int Y);

    public record class ShipCrew(int Current, int Required, int Capacity, string Rotation, int Morale, int Wages);

    public abstract record class ShipCommons(string Symbol, string Name, string Description);

    public abstract record class ShipRequirementsInternals(string Symbol, string Name, string Description, ShipRequirements Requirements)
    : ShipCommons(Symbol, Name, Description);

    public abstract record class ShipInternals(string Symbol, string Name, double Condition, double Integrity, string Description, ShipRequirements Requirements, int Quality)
    : ShipRequirementsInternals(Symbol, Name, Description, Requirements);

    public record class ShipFrame(string Symbol, string Name, double Condition, double Integrity, string Description, int ModuleSlots, int MountingPoints, int FuelCapacity, ShipRequirements Requirements, int Quality)
    : ShipInternals(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipRequirements(int? Power, int? Crew, int? Slots);

    public record class ShipReactor(string Symbol, string Name, double Condition, double Integrity, string Description, int PowerOutput, ShipRequirements Requirements, int Quality)
    : ShipInternals(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipEngine(string Symbol, string Name, double Condition, double Integrity, string Description, int Speed, ShipRequirements Requirements, int Quality)
    : ShipInternals(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipModule(string Symbol, string Name, string Description, int? Capacity, int? Range, ShipRequirements Requirements)
    : ShipRequirementsInternals(Symbol, Name, Description, Requirements);

    public record class ShipMount(string Symbol, string Name, string Description, int? Strength, string[]? Deposits, ShipRequirements Requirements)
    : ShipRequirementsInternals(Symbol, Name, Description, Requirements);

    public record class ShipCargo(int Capacity, int Units, ShipCargoItem[] Inventory);

    public record class ShipCargoItem(string Symbol, string Name, string Description, int Units)
    : ShipCommons(Symbol, Name, Description);

    public record class ShipFuel(int Current, int Capacity, ShipFuelConsumed? Consumed);

    public record class ShipFuelConsumed(int Amount, DateTimeOffset Timestamp);

    public record class ShipCooldown(string ShipSymbol, int TotalSeconds, int RemainingSeconds, DateTimeOffset? Expiration);

    public record class ShipConditionEvent(string Symbol, string Component, string Name, string Description);

    public record class CreateChart(Chart Chart, Waypoint Waypoint, Transaction Transaction, Agent Agent);

    public record class Chart(string WaypointSymbol, string SubmittedBy, DateTimeOffset SubmittedOn);

    public record class Waypoint(string Symbol, string Type, string SystemSymbol, int X, int Y, WaypointOrbital[] Orbitals, string? Orbit, Faction? Faction, WaypointTrait[] Traits, WaypointModifier[]? Modifiers, Chart? Chart, bool IsUnderConstruction);

    public record class WaypointOrbital(string Symbol);

    public record class WaypointTrait(string Symbol, string Name, string Description);

    public record class WaypointModifier(string Symbol, string Name, string Description);

    public record class Transaction(string WaypointSymbol, string ShipSymbol, int TotalPrice, DateTimeOffset Timestamp);
}
