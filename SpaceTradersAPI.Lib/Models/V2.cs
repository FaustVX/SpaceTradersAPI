using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SpaceTradersAPI.Lib.Models;

public static partial class V2
{
    [JsonConverter(typeof(JsonConverter))]
    public readonly struct WaypointSymbol : IEquatable<WaypointSymbol>, IParsable<WaypointSymbol>
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

        public override int GetHashCode()
        => HashCode.Combine(System, Waypoint);

        public override bool Equals([NotNullWhen(true)] object? obj)
        => obj is WaypointSymbol waypoint && Equals(waypoint);

        public bool Equals(WaypointSymbol other)
        => Waypoint == other.Waypoint && System == other.System;

        public static bool operator ==(WaypointSymbol lhs, WaypointSymbol rhs)
        => lhs.Equals(rhs);

        public static bool operator !=(WaypointSymbol lhs, WaypointSymbol rhs)
        => !lhs.Equals(rhs);

        public override readonly string ToString()
        => $"{System}-{Waypoint}";

        public static WaypointSymbol Parse(string s, IFormatProvider? provider)
        => new(s);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out WaypointSymbol result)
        {
            try
            {
                result = new(s!);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }

    [JsonConverter(typeof(JsonConverter))]
    public readonly struct SystemSymbol : IEquatable<SystemSymbol>, IParsable<SystemSymbol>
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

        public override int GetHashCode()
        => HashCode.Combine(Sector, System);

        public override bool Equals([NotNullWhen(true)] object? obj)
        => obj switch
        {
            SystemSymbol sys => Equals(sys),
            WaypointSymbol { System: {} sys } => Equals(sys),
            _ => false,
        };

        public bool Equals(SystemSymbol other)
        => System == other.System && Sector == other.Sector;

        public static bool operator ==(SystemSymbol lhs, SystemSymbol rhs)
        => lhs.Equals(rhs);

        public static bool operator !=(SystemSymbol lhs, SystemSymbol rhs)
        => !lhs.Equals(rhs);

        public override readonly string ToString()
        => $"{Sector}-{System}";

        public static SystemSymbol Parse(string s, IFormatProvider? provider)
        => new(s);

        public static bool TryParse([NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SystemSymbol result)
        {
            try
            {
                result = new(s!);
                return true;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }

    public interface IPosition
    {
        public abstract int X { get; }
        public abstract int Y { get; }
    }

    public interface IAwaitable
    {
        public abstract Task Await();
    }

    public interface IInitWith<TThis, TAccount>
    where TAccount : IAccount
    where TThis : IInitWith<TThis, TAccount>
    {
        public abstract TThis InitWith(TAccount token);
    }

    public record class Agent(string AccountID, string Symbol, WaypointSymbol HeadQuarters, long Credits, FactionSymbol StartingFaction, int ShipCount)
    : IInitWith<Agent, AccountAgent>, IInitWith<Agent, Account>
    {
        private AccountAgent? _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent? AccountAgent { set => _accountAgent ??= value; }

        public Agent InitWith(AccountAgent agent)
        => this with { HeadQuarters = HeadQuarters.InitWith(agent.Account.Accounts), AccountAgent = agent };

        public Agent InitWith(Account account)
        => this with { HeadQuarters = HeadQuarters.InitWith(account) };

        public Task<Responses.Result<IAsyncEnumerable<Ship>>>? ListMyShips()
        => _accountAgent?.API.ListMyShips();

        public Task<Responses.Result<IAsyncEnumerable<Contract>>>? ListMyContracts()
        => _accountAgent?.API.ListMyContracts();

        public Task<Responses.Result<Agent>>? UpdateFromServer()
        => _accountAgent?.API.GetAgent();
    }

    public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships)
    : IInitWith<RegisterAgent, AccountAgent>
    {
        public RegisterAgent InitWith(AccountAgent agent)
        {
            var registration = this with { Agent = Agent.InitWith(agent), Faction = Faction.InitWith(agent.Account.Accounts), Contract = Contract.InitWith(agent) };
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
    : Commons<FactionSymbol>(Symbol, Name, Description), IInitWith<Faction, Account>
    {
        public Faction InitWith(Account account)
        => this with { HeadQuarters = HeadQuarters.InitWith(account) };
    }

    public record class FactionTrait(FactionTraitSymbol Symbol, string Name, string Description)
    : Commons<FactionTraitSymbol>(Symbol, Name, Description);

    public record class Contract(string Id, FactionSymbol FactionSymbol, ContractType Type, ContractTerm Terms, bool Accepted, bool FulFilled, DateTimeOffset DeadlineToAccept)
    : IInitWith<Contract, AccountAgent>
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
        => _accountAgent.API.DeliverCargoToContract(Id, ship.Symbol, trade.ToUpperCase(), units);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Ship ship, ShipCargoItem cargoItem)
        => _accountAgent.API.DeliverCargoToContract(Id, ship.Symbol, cargoItem.Symbol.ToUpperCase(), cargoItem.Units);

        public IAsyncEnumerable<DeliverCargoToContract> DeliverAllCargoToContract(Ship ship)
        => ship.DeliverAllCargoToContract(this);

        public Contract InitWith(AccountAgent agent)
        => this with { Terms = Terms.InitWith(agent.Account.Accounts), AccountAgent = agent };
    }

    public record class AcceptContract(Contract Contract, Agent Agent)
    : IInitWith<AcceptContract, AccountAgent>
    {
        public AcceptContract InitWith(AccountAgent agent)
        => this with { Contract = Contract.InitWith(agent), Agent = Agent.InitWith(agent) };
    }

    public record class DeliverCargoToContract(Contract Contract, ShipCargo Cargo)
    : IInitWith<DeliverCargoToContract, AccountAgent>
    {
        public DeliverCargoToContract InitWith(AccountAgent agent)
        => this with { Contract = Contract.InitWith(agent) };
    }

    public record class ContractTerm(DateTimeOffset Deadline, ContractPayment Payment, ContractDeliverGood[] Deliver)
    : IInitWith<ContractTerm, Account>
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
    : IInitWith<ContractDeliverGood, Account>
    {
        public void Deconstruct(out TradeSymbol tradeSymbol, out WaypointSymbol destinationSymbol, out int unitsRemaining)
        => (tradeSymbol, destinationSymbol, unitsRemaining) = (TradeSymbol, DestinationSymbol, Math.Min(0, UnitsRequired - UnitsFulFilled));

        public ContractDeliverGood InitWith(Account account)
        => this with { DestinationSymbol = DestinationSymbol.InitWith(account) };
    }

    public record class Ship(string Symbol, ShipRegistration Registration, ShipNav Nav, ShipCrew Crew, ShipFrame Frame, ShipReactor Reactor, ShipEngine Engine, ShipModule[] Modules, ShipMount[] Mounts, ShipCargo Cargo, ShipFuel Fuel, ShipCooldown Cooldown)
    : IInitWith<Ship, AccountAgent>
    {
        private AccountAgent _accountAgent = default!;

        [JsonIgnore]
        public AccountAgent AccountAgent { set => _accountAgent ??= value; }
        public AccountAgent GetAgent() => _accountAgent;

        public Ship InitWith(AccountAgent agent)
        => this with { Nav = Nav.InitWith(agent.Account.Accounts), AccountAgent = agent };

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
        => _accountAgent.API.DeliverCargoToContract(contract.Id, Symbol, trade.ToUpperCase(), units);

        public Task<Responses.Result<DeliverCargoToContract>> DeliverCargoToContract(Contract contract, ShipCargoItem cargoItem)
        => _accountAgent.API.DeliverCargoToContract(contract.Id, Symbol, cargoItem.Symbol.ToUpperCase(), cargoItem.Units);

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

        public Task<Responses.Result<ShipNav>> GetNav()
        => _accountAgent.API.GetShipNav(Symbol);

        public Task<Responses.Result<NavigateShip>> PatchNav(ShipNavFlightMode flightMode)
        => _accountAgent.API.PatchShipNav(Symbol, flightMode.ToUpperCase());

        public Task<Responses.Result<RefuelShip>> Refuel()
        => _accountAgent.API.RefuelShip(Symbol);

        public Task<Responses.Result<RefuelShip>> Refuel(int units)
        => _accountAgent.API.RefuelShip(Symbol, units);

        public Task<Responses.Result<RefuelShip>> Refuel(ShipCargoItem cargo)
        => cargo is { Symbol: TradeSymbol.Fuel, Units: > 0 } && Cargo.Get(cargo) is not null ? _accountAgent.API.RefuelShip(Symbol, cargo.Units, fromCargo: true)
        : Task.FromResult(new Responses.Result<RefuelShip>(new Responses.Error($$"""Invalid Cargo, expected: { Symbol: TradeSymbol.Fuel, Units: > 0 }, actual: {{cargo}}""")));

        public (int fuelCost, int duration)? CalculateTravelCost(Waypoint destination)
        {
            // https://github.com/SpaceTradersAPI/api-docs/wiki/Travel-Fuel-and-Time
            if (destination.Symbol.System != Nav.WaypointSymbol.System)
                return null;

            return Nav.Route.Origin.CalculateTravelCost(destination, Nav.FlightMode, Engine.Speed);
        }
    }

    public record class ShipRegistration(string Name, FactionSymbol FactionSymbol, ShipRole Role);

    public record class ShipNav(WaypointSymbol WaypointSymbol, ShipNavRoute Route, ShipNavStatus Status, ShipNavFlightMode FlightMode)
    : IAwaitable, IInitWith<ShipNav, Account>
    {
        public Task Await()
        => Route.Await();

        public ShipNav InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account), Route = Route.InitWith(account) };
    }

    public record class ShipNavRoute(ShipNavRouteWaypoint Destination, ShipNavRouteWaypoint Origin, DateTimeOffset DepartureTime, DateTimeOffset Arrival)
    : IAwaitable, IInitWith<ShipNavRoute, Account>
    {
        public Task Await()
        => Task.Delay(TimeSpan.Max(TimeSpan.Zero, Arrival - DateTimeOffset.Now));

        public ShipNavRoute InitWith(Account account)
        => this with { Destination = Destination.InitWith(account), Origin = Origin.InitWith(account) };
    }

    public record class ShipNavRouteWaypoint(WaypointSymbol Symbol, WaypointType Type, int X, int Y)
    : IPosition, IInitWith<ShipNavRouteWaypoint, Account>
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

    public record class ShipCargo(int Capacity, int Units, ShipCargoItem[] Inventory)
    {
        public ShipCargoItem? Get(ShipCargoItem item)
        => Inventory.FirstOrDefault(i => i.Symbol == item.Symbol && i.Units >= item.Units);
    }

    public record class ShipCargoItem(TradeSymbol Symbol, string Name, string Description, int Units)
    : Commons<TradeSymbol>(Symbol, Name, Description);

    public record class ShipFuel(int Current, int Capacity, ShipFuelConsumed? Consumed);

    public record class ShipFuelConsumed(int Amount, DateTimeOffset Timestamp);

    public record class ShipCooldown(string ShipSymbol, int TotalSeconds, int RemainingSeconds, DateTimeOffset? Expiration)
    : IAwaitable
    {
        public Task Await()
        => Task.Delay(TimeSpan.FromSeconds(RemainingSeconds));
    }

    public record class ShipConditionEvent(ShipConditionEventSymbol Symbol, ShipConditionEventComponent Component, string Name, string Description);

    public record class NavigateShip(ShipNav Nav, ShipFuel Fuel, ShipConditionEvent[] Events)
    : IInitWith<NavigateShip, Account>
    {
        public NavigateShip InitWith(Account account)
        => this with { Nav = Nav.InitWith(account) };
    }

    public record class CreateChart(Chart Chart, Waypoint Waypoint, ChartTransaction Transaction, Agent Agent)
    : IInitWith<CreateChart, AccountAgent>
    {
        public CreateChart InitWith(AccountAgent agent)
        => this with { Chart = Chart.InitWith(agent.Account.Accounts), Waypoint = Waypoint.InitWith(agent.Account.Accounts), Transaction = Transaction.InitWith(agent.Account.Accounts), Agent = Agent.InitWith(agent) };
    }

    public record class Chart(WaypointSymbol WaypointSymbol, FactionSymbol SubmittedBy, DateTimeOffset SubmittedOn)
    : IInitWith<Chart, Account>
    {
        public Chart InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account) };
    }

    public record class Waypoint(WaypointSymbol Symbol, WaypointType Type, int X, int Y, WaypointOrbital[] Orbitals, WaypointSymbol? Orbits, WaypointFaction? Faction, WaypointTrait[] Traits, WaypointModifier[]? Modifiers, Chart? Chart, bool IsUnderConstruction)
    : IPosition, IInitWith<Waypoint, Account>
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
    : IInitWith<WaypointOrbital, Account>
    {
        public WaypointOrbital InitWith(Account account)
        => this with { Symbol = Symbol.InitWith(account) };
    }

    public record class SystemWaypoint(WaypointSymbol Symbol, WaypointType Type, int X, int Y, WaypointOrbital[] Orbitals, WaypointSymbol? Orbits)
    : IPosition, IInitWith<SystemWaypoint, Account>
    {
        public SystemWaypoint InitWith(Account account)
        {
            var waypoint = this with { Symbol = Symbol.InitWith(account), Orbits = Orbits?.InitWith(account) };
            foreach (ref var orbit in waypoint.Orbitals.AsSpan())
                orbit = orbit.InitWith(account);
            return waypoint;
        }
    }

    public record class WaypointTrait(WaypointTraitSymbol Symbol, string Name, string Description)
    : Commons<WaypointTraitSymbol>(Symbol, Name, Description);

    public record class WaypointModifier(WaypointModifierSymbol Symbol, string Name, string Description)
    : Commons<WaypointModifierSymbol>(Symbol, Name, Description);

    public record class ChartTransaction(WaypointSymbol WaypointSymbol, string ShipSymbol, int TotalPrice, DateTimeOffset Timestamp)
    : IInitWith<ChartTransaction, Account>
    {
        public ChartTransaction InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account) };
    }

    public record class System(SystemSymbol Symbol, SystemType Type, int X, int Y, string? Constellation, string? Name, WaypointFaction[] Factions, SystemWaypoint[] Waypoints)
    : IPosition, IInitWith<System, Account>
    {
        public System InitWith(Account account)
        {
            var system = this with { Symbol = Symbol.InitWith(account)};
            foreach (ref var waypoint in system.Waypoints.AsSpan())
                waypoint = waypoint.InitWith(account);
            return system;
        }
    }

    public record class Transaction(WaypointSymbol WaypointSymbol, string ShipSymbol, TradeSymbol TradeSymbol, TransactionType Type, int Units, int PricePerUnit, int TotalPrice, DateTimeOffset Timestamp)
    : IInitWith<Transaction, Account>
    {
        public Transaction InitWith(Account account)
        => this with { WaypointSymbol = WaypointSymbol.InitWith(account) };
    }

    public record class RefuelShip(Agent Agent, ShipFuel Fuel, ShipCargo Cargo, Transaction Transaction)
    : IInitWith<RefuelShip, AccountAgent>
    {
        public RefuelShip InitWith(AccountAgent agent)
        => this with { Agent = Agent.InitWith(agent) };
    }
}
