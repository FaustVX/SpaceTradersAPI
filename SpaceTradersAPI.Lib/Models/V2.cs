using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTradersAPI.Lib.Models;

public static partial class V2
{
    [JsonConverter(typeof(JsonConverter))]
    public readonly struct WaypointSymbol
    {
        private class JsonConverter : JsonConverter<WaypointSymbol>
        {
            public override WaypointSymbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

            public override void Write(Utf8JsonWriter writer, WaypointSymbol value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
        }
        private readonly Account _account = default!;

        [JsonIgnore]
        public Account Account { init => _account ??= value; }

        public SystemSymbol System { get; init; }
        public string Waypoint { get; init; }

        public WaypointSymbol InitWith(Account account)
        => this with { Account = account, System = System.InitWith(account) };

        public Task<Responses.Result<Waypoint>> GetWaypointData()
        => _account.API.GetWaypoint(this);

        public WaypointSymbol(string symbol)
        {
            var ranges = (stackalloc Range[3]);
            switch (ranges[..symbol.AsSpan().Split(ranges, '-')])
            {
                case [var sector, var system, var waypoint]:
                    System = new() { Sector = symbol[sector], System = symbol[system], Account = _account };
                    Waypoint = symbol[waypoint];
                    break;
                default: throw new UnreachableException();
            }
        }

        public override readonly string ToString()
        => this switch
        {
            { System: SystemSymbol system, Waypoint: string waypoint } => $"{system}-{waypoint}",
            { System: SystemSymbol system } => system.ToString(),
        };
    }

    [JsonConverter(typeof(JsonConverter))]
    public readonly struct SystemSymbol
    {
        private class JsonConverter : JsonConverter<SystemSymbol>
        {
            public override SystemSymbol Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => new(reader.GetString()!);

            public override void Write(Utf8JsonWriter writer, SystemSymbol value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToString());
        }
        private readonly Account _account = default!;

        [JsonIgnore]
        public Account Account { init => _account ??= value; }

        public string Sector { get; init; }
        public string System { get; init; }

        public SystemSymbol(string symbol)
        {
            var ranges = (stackalloc Range[3]);
            switch (ranges[..symbol.AsSpan().Split(ranges, '-')])
            {
                case [var sector, var system]:
                    Sector = symbol[sector];
                    System = symbol[system];
                    break;
                default: throw new UnreachableException();
            }
        }

        public WaypointSymbol CreateLocalWaypoint(string localWaypoint)
        => new() { System = this, Waypoint = localWaypoint, Account = _account };

        public SystemSymbol InitWith(Account account)
        => this with { Account = account };

        public override readonly string ToString()
        => $"{Sector}-{System}";
    }

    public interface IPosition
    {
        public abstract int X { get; }
        public abstract int Y { get; }
    }

    public record class Agent(string AccountID, string Symbol, WaypointSymbol HeadQuarters, long Credits, FactionSymbol StartingFaction, int ShipCount)
    {
        private AccountAgent? _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent? AccountAgent { set => _accountAgent ??= value; }

        public Agent InitWith(AccountAgent agent)
        => this with { HeadQuarters = HeadQuarters.InitWith(agent.Accounts), AccountAgent = agent };

        public Agent InitWith(Account account)
        => this with { HeadQuarters = HeadQuarters.InitWith(account) };

        public IAsyncEnumerable<Ship>? ListMyShips()
        => _accountAgent?.API.ListMyShips();

        public IAsyncEnumerable<Contract>? ListMyContracts()
        => _accountAgent?.API.ListMyContracts();

        public Task<Responses.Result<Agent>>? UpdateFromServer()
        => _accountAgent?.API.GetAgent();
    }

    public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships)
    {
        public RegisterAgent InitWith(AccountAgent agent)
        {
            var registration = this with { Agent = Agent.InitWith(agent), Faction = Faction.InitWith(agent.Accounts), Contract = Contract.InitWith(agent) };
            foreach (ref var ship in registration.Ships.AsSpan())
                ship = ship.InitWith(agent);
            return registration;
        }
    }

    public record class AgentFaction(FactionSymbol Symbol, int Reputation);

    public record class ServerStatus(string Status, string Version, DateOnly ResetDate, ServerStatusHealth Health, ServerStatusResets ServerResets);

    public record class ServerStatusHealth(DateTimeOffset LastMarketUpdate);

    public record class ServerStatusResets(DateTimeOffset Next, string Frequency);

    public record class Faction(FactionSymbol Symbol, string Name, string Description, SystemSymbol HeadQuarters, FactionTrait[] Traits, bool IsRecruiting)
    : Commons<FactionSymbol>(Symbol, Name, Description)
    {
        public Faction InitWith(Account account)
        => this with { HeadQuarters = HeadQuarters.InitWith(account) };
    }

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

        public Contract InitWith(AccountAgent agent)
        => this with { Terms = Terms.InitWith(agent.Accounts), AccountAgent = agent };
    }

    public record class AcceptContract(Contract Contract, Agent Agent)
    {
        public AcceptContract InitWith(AccountAgent agent)
        => this with { Contract = Contract.InitWith(agent), Agent = Agent.InitWith(agent) };
    }

    public record class DeliverCargoToContract(Contract Contract, ShipCargo Cargo)
    {
        public DeliverCargoToContract InitWith(AccountAgent agent)
        => this with { Contract = Contract.InitWith(agent) };
    }

    public record class ContractTerm(DateTimeOffset Deadline, ContractPayment Payment, ContractDeliverGood[] Deliver)
    {
        public ContractTerm InitWith(Account account)
        {
            foreach (ref var deliver in Deliver.AsSpan())
                deliver = deliver.InitWith(account);
            return this;
        }
    }

    public record class ContractPayment(int OnAccepted, int OnFulfilled);

    public record class ContractDeliverGood(TradeSymbol TradeSymbol, WaypointSymbol DestinationSymbol, int UnitsRequired, int UnitsFulFilled)
    {
        public void Deconstruct(out TradeSymbol tradeSymbol, out WaypointSymbol destinationSymbol, out int unitsRemaining)
        => (tradeSymbol, destinationSymbol, unitsRemaining) = (TradeSymbol, DestinationSymbol, Math.Min(0, UnitsRequired - UnitsFulFilled));

        public ContractDeliverGood InitWith(Account account)
        => this with { DestinationSymbol = DestinationSymbol.InitWith(account) };
    }

    public record class Ship(string Symbol, ShipRegistration Registration, ShipNav Nav, ShipCrew Crew, ShipFrame Frame, ShipReactor Reactor, ShipEngine Engine, ShipModule[] Modules, ShipMount[] Mounts, ShipCargo Cargo, ShipFuel Fuel, ShipCooldown Cooldown)
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }
        public AccountAgent GetAgent() => _accountAgent;

        public Ship InitWith(AccountAgent agent)
        => this with { Nav = Nav.InitWith(agent.Accounts), AccountAgent = agent };

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

        public Task<Responses.Result<NavigateShip>> Navigate(WaypointSymbol waypointSymbol)
        => _accountAgent.API.NavigateShip(Symbol, waypointSymbol.ToString());

        public WaypointSymbol CreateLocation(string localWaypoint)
        => Nav.WaypointSymbol.System.CreateLocalWaypoint(localWaypoint);
    }

    public record class ShipRegistration(string Name, FactionSymbol FactionSymbol, ShipRole Role);

    public record class ShipNav(WaypointSymbol WaypointSymbol, ShipNavRoute Route, ShipNavStatus Status, ShipNavFlightMode FlightMode)
    {
        public ShipNav InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account), Route = Route.InitWith(account) };
    }

    public record class ShipNavRoute(ShipNavRouteWaypoint Destination, ShipNavRouteWaypoint Origin, DateTimeOffset DepartureTime, DateTimeOffset Arrival)
    {
        public Task Await()
        => Task.Delay(TimeSpan.Max(TimeSpan.Zero, Arrival - DateTimeOffset.Now));

        public ShipNavRoute InitWith(Account account)
        => this with { Destination = Destination.InitWith(account), Origin = Origin.InitWith(account) };
    }

    public record class ShipNavRouteWaypoint(WaypointSymbol Symbol, WaypointType Type, int X, int Y) : IPosition
    {
        public ShipNavRouteWaypoint InitWith(Account account)
        => this with { Symbol = Symbol.InitWith(account) };
    }

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

    public record class NavigateShip(ShipNav Nav, ShipFuel Fuel, ShipConditionEvent[] Events)
    {
        public NavigateShip InitWith(Account account)
        => this with { Nav = Nav.InitWith(account) };
    }

    public record class CreateChart(Chart Chart, Waypoint Waypoint, ChartTransaction Transaction, Agent Agent)
    {
        public CreateChart InitWith(AccountAgent agent)
        => this with { Chart = Chart.InitWith(agent.Accounts), Waypoint = Waypoint.InitWith(agent.Accounts), Transaction = Transaction.InitWith(agent.Accounts), Agent = Agent.InitWith(agent) };
    }

    public record class Chart(WaypointSymbol WaypointSymbol, FactionSymbol SubmittedBy, DateTimeOffset SubmittedOn)
    {
        public Chart InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account) };
    }

    public record class Waypoint(WaypointSymbol Symbol, WaypointType Type, int X, int Y, WaypointOrbital[] Orbitals, WaypointSymbol? Orbits, WaypointFaction? Faction, WaypointTrait[] Traits, WaypointModifier[]? Modifiers, Chart? Chart, bool IsUnderConstruction) : IPosition
    {
        public Waypoint InitWith(Account account)
        {
            var waypoint = this with { Symbol = Symbol.InitWith(account), Chart = Chart?.InitWith(account), Orbits = Orbits?.InitWith(account) };
            foreach (ref var orbital in Orbitals.AsSpan())
                orbital = orbital.InitWith(account);
            return waypoint;
        }
    }

    public record class WaypointFaction(FactionSymbol Symbol);

    public record class WaypointOrbital(WaypointSymbol Symbol)
    {
        public WaypointOrbital InitWith(Account account)
        => this with { Symbol = Symbol.InitWith(account) };
    }

    public record class WaypointTrait(WaypointTraitSymbol Symbol, string Name, string Description)
    : Commons<WaypointTraitSymbol>(Symbol, Name, Description);

    public record class WaypointModifier(WaypointModifierSymbol Symbol, string Name, string Description)
    : Commons<WaypointModifierSymbol>(Symbol, Name, Description);

    public record class ChartTransaction(WaypointSymbol WaypointSymbol, string ShipSymbol, int TotalPrice, DateTimeOffset Timestamp)
    {
        public ChartTransaction InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account) };
    }
}
