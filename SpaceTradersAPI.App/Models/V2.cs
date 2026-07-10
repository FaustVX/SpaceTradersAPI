using System.Text.Json.Serialization;

namespace SpaceTradersAPI.App.Models;

public static class V2
{
    public record class Agent(string AccountID, string Symbol, string HeadQuarters, long Credits, FactionSymbol StartingFaction, int ShipCount)
    {
        private AccountItem _account = default!;

        [JsonIgnore]
        public AccountItem AccountAgent { set => _account ??= value; }

        public Task<Responses.Result<RegisterAgent>> RegisterAgent(string symbol, FactionSymbol faction)
        => _account.API.RegisterAgent(symbol, faction);
    }

    public record class RegisterAgent(string Token, Agent Agent, Faction Faction, Contract Contract, Ship[] Ships);

    public record class AgentFaction(FactionSymbol Symbol, int Reputation);

    public record class ServerStatus(string Status, string Version, DateOnly ResetDate, ServerStatusHealth Health, ServerStatusResets ServerResets);

    public record class ServerStatusHealth(DateTimeOffset LastMarketUpdate);

    public record class ServerStatusResets(DateTimeOffset Next, string Frequency);

    public record class Faction(FactionSymbol Symbol, string Name, string Description, string HeadQuarters, FactionTrait[] Traits, bool IsRecruiting);

    public record class FactionTrait(FactionTraitSymbol Symbol, string Name, string Description);

    public record class Contract(string Id, FactionSymbol FactionSymbol, ContractType Type, ContractTerm Terms, bool Accepted, bool FulFilled, DateTimeOffset DeadlineToAccept);

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

    public record class ShipCooldown(string ShipSymbol, int TotalSeconds, int RemainingSeconds, DateTimeOffset? Expiration);

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

    public enum ShipRole
    {
        Fabricator,
        Harvester,
        Hauler,
        Interceptor,
        Excavator,
        Transport,
        Repair,
        Surveyor,
        Command,
        Carrier,
        Patrol,
        Satellite,
        Explorer,
        Refinery,
    }

    public enum FactionSymbol
    {
        Cosmic,
        Void,
        Galactic,
        Quantum,
        Dominion,
        Astro,
        Corsairs,
        Obsidian,
        Aegis,
        United,
        Solitary,
        Cobalt,
        Omega,
        Echo,
        Lords,
        Cult,
        Ancients,
        Shadow,
        Ethereal,
    }

    public enum FactionTraitSymbol
    {
        Bureaucratic,
        Secretive,
        Capitalistic,
        Industrious,
        Peaceful,
        Distrustful,
        Welcoming,
        Smugglers,
        Scavengers,
        Rebellious,
        Exiles,
        Pirates,
        Raiders,
        Clan,
        Guild,
        Dominion,
        Fringe,
        Forsaken,
        Isolated,
        Localized,
        Established,
        Notable,
        Dominant,
        Inescapable,
        Innovative,
        Bold,
        Visionary,
        Curious,
        Daring,
        Exploratory,
        Resourceful,
        Flexible,
        Cooperative,
        United,
        Strategic,
        Intelligent,
        Research_Focused,
        Collaborative,
        Progressive,
        Militaristic,
        Technologically_Advanced,
        Aggressive,
        Imperialistic,
        Treasure_Hunters,
        Dexterous,
        Unpredictable,
        Brutal,
        Fleeting,
        Adaptable,
        Self_Sufficient,
        Defensive,
        Proud,
        Diverse,
        Independent,
        Self_Interested,
        Fragmented,
        Commercial,
        Free_Markets,
        Entrepreneurial,
    }

    public enum TradeSymbol
    {
        Precious_Stones,
        Quartz_Sand,
        Silicon_Crystals,
        Ammonia_Ice,
        Liquid_Hydrogen,
        Liquid_Nitrogen,
        Ice_Water,
        Exotic_Matter,
        Advanced_Circuitry,
        Graviton_Emitters,
        Iron,
        Iron_Ore,
        Copper,
        Copper_Ore,
        Aluminum,
        Aluminum_Ore,
        Silver,
        Silver_Ore,
        Gold,
        Gold_Ore,
        Platinum,
        Platinum_Ore,
        Diamonds,
        Uranite,
        Uranite_Ore,
        Meritium,
        Meritium_Ore,
        Hydrocarbon,
        Antimatter,
        Fab_Mats,
        Fertilizers,
        Fabrics,
        Food,
        Jewelry,
        Machinery,
        Firearms,
        Assault_Rifles,
        Military_Equipment,
        Explosives,
        Lab_Instruments,
        Ammunition,
        Electronics,
        Ship_Plating,
        Ship_Parts,
        Equipment,
        Fuel,
        Medicine,
        Drugs,
        Clothing,
        Microprocessors,
        Plastics,
        Polynucleotides,
        Biocomposites,
        Quantum_Stabilizers,
        Nanobots,
        Ai_Mainframes,
        Quantum_Drives,
        Robotic_Drones,
        Cyber_Implants,
        Gene_Therapeutics,
        Neural_Chips,
        Mood_Regulators,
        Viral_Agents,
        Micro_Fusion_Generators,
        Supergrains,
        Laser_Rifles,
        Holographics,
        Ship_Salvage,
        Relic_Tech,
        Novel_Lifeforms,
        Botanical_Specimens,
        Cultural_Artifacts,
        Frame_Probe,
        Frame_Drone,
        Frame_Interceptor,
        Frame_Racer,
        Frame_Fighter,
        Frame_Frigate,
        Frame_Shuttle,
        Frame_Explorer,
        Frame_Miner,
        Frame_Light_Freighter,
        Frame_Heavy_Freighter,
        Frame_Transport,
        Frame_Destroyer,
        Frame_Cruiser,
        Frame_Carrier,
        Frame_Bulk_Freighter,
        Reactor_Solar_I,
        Reactor_Fusion_I,
        Reactor_Fission_I,
        Reactor_Chemical_I,
        Reactor_Antimatter_I,
        Engine_Impulse_Drive_I,
        Engine_Ion_Drive_I,
        Engine_Ion_Drive_II,
        Engine_Hyper_Drive_I,
        Module_Mineral_Processor_I,
        Module_Gas_Processor_I,
        Module_Cargo_Hold_I,
        Module_Cargo_Hold_II,
        Module_Cargo_Hold_III,
        Module_Crew_Quarters_I,
        Module_Envoy_Quarters_I,
        Module_Passenger_Cabin_I,
        Module_Micro_Refinery_I,
        Module_Science_Lab_I,
        Module_Jump_Drive_I,
        Module_Jump_Drive_II,
        Module_Jump_Drive_III,
        Module_Warp_Drive_I,
        Module_Warp_Drive_II,
        Module_Warp_Drive_III,
        Module_Shield_Generator_I,
        Module_Shield_Generator_II,
        Module_Ore_Refinery_I,
        Module_Fuel_Refinery_I,
        Mount_Gas_Siphon_I,
        Mount_Gas_Siphon_II,
        Mount_Gas_Siphon_III,
        Mount_Surveyor_I,
        Mount_Surveyor_II,
        Mount_Surveyor_III,
        Mount_Sensor_Array_I,
        Mount_Sensor_Array_II,
        Mount_Sensor_Array_III,
        Mount_Mining_Laser_I,
        Mount_Mining_Laser_II,
        Mount_Mining_Laser_III,
        Mount_Laser_Cannon_I,
        Mount_Missile_Launcher_I,
        Mount_Turret_I,
        Ship_Probe,
        Ship_Mining_Drone,
        Ship_Siphon_Drone,
        Ship_Interceptor,
        Ship_Light_Hauler,
        Ship_Command_Frigate,
        Ship_Explorer,
        Ship_Heavy_Freighter,
        Ship_Light_Shuttle,
        Ship_Ore_Hound,
        Ship_Refining_Freighter,
        Ship_Surveyor,
        Ship_Bulk_Freighter,
    }

    public enum ShipMountSymbol
    {
        Mount_Gas_Siphon_I,
        Mount_Gas_Siphon_II,
        Mount_Gas_Siphon_III,
        Mount_Surveyor_I,
        Mount_Surveyor_II,
        Mount_Surveyor_III,
        Mount_Sensor_Array_I,
        Mount_Sensor_Array_II,
        Mount_Sensor_Array_III,
        Mount_Mining_Laser_I,
        Mount_Mining_Laser_II,
        Mount_Mining_Laser_III,
        Mount_Laser_Cannon_I,
        Mount_Missile_Launcher_I,
        Mount_Turret_I,
    }

    public enum ShipModuleSymbol
    {
        Module_Mineral_Processor_I,
        Module_Gas_Processor_I,
        Module_Cargo_Hold_I,
        Module_Cargo_Hold_II,
        Module_Cargo_Hold_III,
        Module_Crew_Quarters_I,
        Module_Envoy_Quarters_I,
        Module_Passenger_Cabin_I,
        Module_Micro_Refinery_I,
        Module_Ore_Refinery_I,
        Module_Fuel_Refinery_I,
        Module_Science_Lab_I,
        Module_Jump_Drive_I,
        Module_Jump_Drive_II,
        Module_Jump_Drive_III,
        Module_Warp_Drive_I,
        Module_Warp_Drive_II,
        Module_Warp_Drive_III,
        Module_Shield_Generator_I,
        Module_Shield_Generator_II,
    }

    public enum ShipEngineSymbol
    {
        Engine_Impulse_Drive_I,
        Engine_Ion_Drive_I,
        Engine_Ion_Drive_II,
        Engine_Hyper_Drive_I,
    }

    public enum ShipReactorSymbol
    {
        Reactor_Solar_I,
        Reactor_Fusion_I,
        Reactor_Fission_I,
        Reactor_Chemical_I,
        Reactor_Antimatter_I,
    }

    public enum ShipFrameSymbol
    {
        Frame_Probe,
        Frame_Drone,
        Frame_Interceptor,
        Frame_Racer,
        Frame_Fighter,
        Frame_Frigate,
        Frame_Shuttle,
        Frame_Explorer,
        Frame_Miner,
        Frame_Light_Freighter,
        Frame_Heavy_Freighter,
        Frame_Transport,
        Frame_Destroyer,
        Frame_Cruiser,
        Frame_Carrier,
        Frame_Bulk_Freighter,
    }

    public enum WaypointType
    {
        Planet,
        Gas_Giant,
        Moon,
        Orbital_Station,
        Jump_Gate,
        Asteroid_Field,
        Asteroid,
        Engineered_Asteroid,
        Asteroid_Base,
        Nebula,
        Debris_Field,
        Gravity_Well,
        Artificial_Gravity_Well,
        Fuel_Station,
    }

    public enum ShipNavStatus
    {
        In_Transit,
        In_Orbit,
        Docked,
    }

    public enum ShipNavFlightMode
    {
        Drift,
        Stealth,
        Cruise,
        Burn,
    }

    public enum ShipCrewRotation
    {
        Strict,
        Relaxed,
    }

    public enum ContractType
    {
        Procurement,
        Transport,
        Shuttle,
    }

    public enum ShipConditionEventSymbol
    {
        Reactor_Overload,
        Energy_Spike_From_Mineral,
        Solar_Flare_Interference,
        Coolant_Leak,
        Power_Distribution_Fluctuation,
        Magnetic_Field_Disruption,
        Hull_Micrometeorite_Strikes,
        Structural_Stress_Fractures,
        Corrosive_Mineral_Contamination,
        Thermal_Expansion_Mismatch,
        Vibration_Damage_From_Drilling,
        Electromagnetic_Field_Interference,
        Impact_With_Extracted_Debris,
        Fuel_Efficiency_Degradation,
        Coolant_System_Ageing,
        Dust_Microabrasions,
        Thruster_Nozzle_Wear,
        Exhaust_Port_Clogging,
        Bearing_Lubrication_Fade,
        Sensor_Calibration_Drift,
        Hull_Micrometeorite_Damage,
        Space_Debris_Collision,
        Thermal_Stress,
        Vibration_Overload,
        Pressure_Differential_Stress,
        Electromagnetic_Surge_Effects,
        Atmospheric_Entry_Heat,
    }

    public enum ShipConditionEventComponent
    {
        Frame,
        Reactor,
        Engine,
    }

    public enum WaypointTraitSymbol
    {
        Uncharted,
        Under_Construction,
        Marketplace,
        Shipyard,
        Outpost,
        Scattered_Settlements,
        Sprawling_Cities,
        Mega_Structures,
        Pirate_Base,
        Overcrowded,
        High_Tech,
        Corrupt,
        Bureaucratic,
        Trading_Hub,
        Industrial,
        Black_Market,
        Research_Facility,
        Military_Base,
        Surveillance_Outpost,
        Exploration_Outpost,
        Mineral_Deposits,
        Common_Metal_Deposits,
        Precious_Metal_Deposits,
        Rare_Metal_Deposits,
        Methane_Pools,
        Ice_Crystals,
        Explosive_Gases,
        Strong_Magnetosphere,
        Vibrant_Auroras,
        Salt_Flats,
        Canyons,
        Perpetual_Daylight,
        Perpetual_Overcast,
        Dry_Seabeds,
        Magma_Seas,
        Supervolcanoes,
        Ash_Clouds,
        Vast_Ruins,
        Mutated_Flora,
        Terraformed,
        Extreme_Temperatures,
        Extreme_Pressure,
        Diverse_Life,
        Scarce_Life,
        Fossils,
        Weak_Gravity,
        Strong_Gravity,
        Crushing_Gravity,
        Toxic_Atmosphere,
        Corrosive_Atmosphere,
        Breathable_Atmosphere,
        Thin_Atmosphere,
        Jovian,
        Rocky,
        Volcanic,
        Frozen,
        Swamp,
        Barren,
        Temperate,
        Jungle,
        Ocean,
        Radioactive,
        Micro_Gravity_Anomalies,
        Debris_Cluster,
        Deep_Craters,
        Shallow_Craters,
        Unstable_Composition,
        Hollowed_Interior,
        Stripped,
    }

    public enum WaypointModifierSymbol
    {
        Stripped,
        Unstable,
        Radiation_Leak,
        Critical_Limit,
        Civil_Unrest,
    }

}
