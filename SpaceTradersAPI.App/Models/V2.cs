using System.Text.Json.Serialization;

namespace SpaceTradersAPI.App.Models;

public static partial class V2
{
    public record class Agent(string AccountID, string Symbol, string HeadQuarters, long Credits, FactionSymbol StartingFaction, int ShipCount)
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }

        public IAsyncEnumerable<Ship> ListMyShips()
        => _accountAgent.API.ListMyShips();

        public IAsyncEnumerable<Contract> ListMyContracts()
        => _accountAgent.API.ListMyContracts();

        public Task<Responses.Result<Agent>> UpdateFromServer()
        => _accountAgent.API.GetAgent();
    }

    public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships);

    public record class AgentFaction(FactionSymbol Symbol, int Reputation);

    public record class ServerStatus(string Status, string Version, DateOnly ResetDate, ServerStatusHealth Health, ServerStatusResets ServerResets);

    public record class ServerStatusHealth(DateTimeOffset LastMarketUpdate);

    public record class ServerStatusResets(DateTimeOffset Next, string Frequency);

    public record class Faction(FactionSymbol Symbol, string Name, string Description, string HeadQuarters, FactionTrait[] Traits, bool IsRecruiting)
    : Commons<FactionSymbol>(Symbol, Name, Description);

    public record class FactionTrait(FactionTraitSymbol Symbol, string Name, string Description)
    : Commons<FactionTraitSymbol>(Symbol, Name, Description);

    public record class Contract(string Id, FactionSymbol FactionSymbol, ContractType Type, ContractTerm Terms, bool Accepted, bool FulFilled, DateTimeOffset DeadlineToAccept)
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }

        public Task<Responses.Result<AcceptContract>> AcceptContract()
        => _accountAgent.API.AcceptContract(Id);

        public Task<Responses.Result<Contract>> UpdateFromServer()
        => _accountAgent.API.GetContract(Id);

        public Task<Responses.Result<AcceptContract>> FulfillContract()
        => _accountAgent.API.FulfillContract(Id);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Ship ship, TradeSymbol trade, int units)
        => _accountAgent.API.DeliverCargoToContract(Id, ship.Symbol, trade, units);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Ship ship, ShipCargoItem cargoItem)
        => _accountAgent.API.DeliverCargoToContract(Id, ship.Symbol, cargoItem.Symbol, cargoItem.Units);

        public IAsyncEnumerable<DeliverCargoToContract> DeliverAllCargoToContract(Ship ship)
        => ship.DeliverAllCargoToContract(this);
    }

    public record class AcceptContract(Contract Contract, Agent Agent);

    public record class DeliverCargoToContract(Contract Contract, ShipCargo Cargo);

    public record class ContractTerm(DateTimeOffset Deadline, ContractPayment Payment, ContractDeliverGood[] Deliver);

    public record class ContractPayment(int OnAccepted, int OnFulfilled);

    public record class ContractDeliverGood(TradeSymbol TradeSymbol, string DestinationSymbol, int UnitsRequired, int UnitsFulFilled)
    {
        public void Deconstruct(out TradeSymbol tradeSymbol, out string destinationSymbol, out int unitsRemaining)
        => (tradeSymbol, destinationSymbol, unitsRemaining) = (TradeSymbol, DestinationSymbol, Math.Min(0, UnitsRequired - UnitsFulFilled));
    }

    public record class Ship(string Symbol, ShipRegistration Registration, ShipNav Nav, ShipCrew Crew, ShipFrame Frame, ShipReactor Reactor, ShipEngine Engine, ShipModule[] Modules, ShipMount[] Mounts, ShipCargo Cargo, ShipFuel Fuel, ShipCooldown Cooldown)
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }
        public AccountAgent GetAgent() => _accountAgent;
        public Task<Responses.Result<ShipNav>> Dock()
        => _accountAgent.API.DockShip(Symbol);

        public Task<Responses.Result<ShipNav>> Orbit()
        => _accountAgent.API.OrbitShip(Symbol);

        public Task<Responses.Result<CreateChart>> CreateChart()
        => _accountAgent.API.CreateChart(Symbol);

        public Task<Responses.Result<Ship>> UpdateFromServer()
        => _accountAgent.API.GetShip(Symbol);

        public Task<Responses.Result<Contract>> NegociateContract()
        => _accountAgent.API.NegociateContract(Symbol);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Contract contract, TradeSymbol trade, int units)
        => _accountAgent.API.DeliverCargoToContract(contract.Id, Symbol, trade, units);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Contract contract, ShipCargoItem cargoItem)
        => _accountAgent.API.DeliverCargoToContract(contract.Id, Symbol, cargoItem.Symbol, cargoItem.Units);

        public async IAsyncEnumerable<DeliverCargoToContract> DeliverAllCargoToContract(Contract contract)
        {
            foreach (var (trade, _, remaining) in contract.Terms.Deliver)
                if (GetCargo(Cargo, trade, remaining) is {} cargo)
                    yield return await DeliverCargoToContract(contract, cargo).ValueOrThrowAsync();

            static ShipCargoItem? GetCargo(ShipCargo shipCargo, TradeSymbol tradeSymbol, int units)
            {
                foreach (var cargo in shipCargo.Inventory)
                    if (cargo.Symbol == tradeSymbol && cargo.Units >= units)
                        return cargo;
                return null;
            }
        }
    }

    public record class ShipRegistration(string Name, FactionSymbol FactionSymbol, ShipRole Role);

    public record class ShipNav(string SystemSymbol, string WaypointSymbol, ShipNavRoute Route, ShipNavStatus Status, ShipNavFlightMode FlightMode);

    public record class ShipNavRoute(ShipNavRouteWaypoint Destination, ShipNavRouteWaypoint Origin, DateTimeOffset DepartureTime, DateTimeOffset Arrival);

    public record class ShipNavRouteWaypoint(string Symbol, WaypointType Type, string SystemSymbol, int X, int Y);

    public record class ShipCrew(int Current, int Required, int Capacity, ShipCrewRotation Rotation, int Morale, int Wages);

    public abstract record class Commons<T>(T Symbol, string Name, string Description);

    public abstract record class ShipRequirementsInternals<T>(T Symbol, string Name, string Description, ShipRequirements Requirements)
    : Commons<T>(Symbol, Name, Description);

    public abstract record class ShipInternals<T>(T Symbol, string Name, double Condition, double Integrity, string Description, ShipRequirements Requirements, int Quality)
    : ShipRequirementsInternals<T>(Symbol, Name, Description, Requirements);

    public record class ShipFrame(ShipFrameSymbol Symbol, string Name, double Condition, double Integrity, string Description, int ModuleSlots, int MountingPoints, int FuelCapacity, ShipRequirements Requirements, int Quality)
    : ShipInternals<ShipFrameSymbol>(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipRequirements(int? Power, int? Crew, int? Slots);

    public record class ShipReactor(ShipReactorSymbol Symbol, string Name, double Condition, double Integrity, string Description, int PowerOutput, ShipRequirements Requirements, int Quality)
    : ShipInternals<ShipReactorSymbol>(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipEngine(ShipEngineSymbol Symbol, string Name, double Condition, double Integrity, string Description, int Speed, ShipRequirements Requirements, int Quality)
    : ShipInternals<ShipEngineSymbol>(Symbol, Name, Condition, Integrity, Description, Requirements, Quality);

    public record class ShipModule(ShipModuleSymbol Symbol, string Name, string Description, int? Capacity, int? Range, ShipRequirements Requirements)
    : ShipRequirementsInternals<ShipModuleSymbol>(Symbol, Name, Description, Requirements);

    public record class ShipMount(ShipMountSymbol Symbol, string Name, string Description, int? Strength, string[]? Deposits, ShipRequirements Requirements)
    : ShipRequirementsInternals<ShipMountSymbol>(Symbol, Name, Description, Requirements);

    public record class ShipCargo(int Capacity, int Units, ShipCargoItem[] Inventory);

    public record class ShipCargoItem(TradeSymbol Symbol, string Name, string Description, int Units)
    : Commons<TradeSymbol>(Symbol, Name, Description);

    public record class ShipFuel(int Current, int Capacity, ShipFuelConsumed? Consumed);

    public record class ShipFuelConsumed(int Amount, DateTimeOffset Timestamp);

    public record class ShipCooldown(string ShipSymbol, int TotalSeconds, int RemainingSeconds, DateTimeOffset? Expiration)
    {
        public Task Await()
        => Task.Delay(TimeSpan.FromSeconds(RemainingSeconds));
    }

    public record class ShipConditionEvent(ShipConditionEventSymbol Symbol, ShipConditionEventComponent Component, string Name, string Description);

    public record class CreateChart(Chart Chart, Waypoint Waypoint, Transaction Transaction, Agent Agent);

    public record class Chart(string WaypointSymbol, string SubmittedBy, DateTimeOffset SubmittedOn);

    public record class Waypoint(string Symbol, WaypointType Type, string SystemSymbol, int X, int Y, WaypointOrbital[] Orbitals, string? Orbit, Faction? Faction, WaypointTrait[] Traits, WaypointModifier[]? Modifiers, Chart? Chart, bool IsUnderConstruction);

    public record class WaypointOrbital(string Symbol);

    public record class WaypointTrait(WaypointTraitSymbol Symbol, string Name, string Description)
    : Commons<WaypointTraitSymbol>(Symbol, Name, Description);

    public record class WaypointModifier(WaypointModifierSymbol Symbol, string Name, string Description)
    : Commons<WaypointModifierSymbol>(Symbol, Name, Description);

    public record class Transaction(string WaypointSymbol, string ShipSymbol, int TotalPrice, DateTimeOffset Timestamp);
}
