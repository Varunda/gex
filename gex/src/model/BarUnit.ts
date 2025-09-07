
export class ApiBarUnit {

    public definitionName: string = "";
    public displayName: string = "";
    public description: string = "";
    public unit: BarUnit = new BarUnit();

    public includedWeapons: BarWeaponDefinition[] = [];
    public includedUnits: BarUnit[] = [];

    public static parse(elem: any): ApiBarUnit {
        const ret: ApiBarUnit = {
            definitionName: elem.definitionName,
            displayName: elem.displayName,
            description: elem.description,
            unit: BarUnit.parse(elem.unit),
            includedWeapons: elem.includedWeapons.map((iter: any) => BarWeaponDefinition.parse(iter)),
            includedUnits: elem.includedUnits.map((iter: any) => BarUnit.parse(iter))
        };

        if (ret.unit.explodeAs != "") {
            ret.unit.explodeAsWeapon = ret.includedWeapons.find(iter => iter.definitionName.toLowerCase() == ret.unit.explodeAs.toLowerCase()) || new BarWeaponDefinition();
        }
        if (ret.unit.selfDestructWeapon != "") {
            ret.unit.selfDestructWeaponDefinition = ret.includedWeapons.find(iter => iter.definitionName.toLowerCase() == ret.unit.selfDestructWeapon.toLowerCase()) || new BarWeaponDefinition();
            ret.unit.selfDestructDamage = ret.unit.selfDestructWeaponDefinition?.damages.get("default") ?? 0;
        }

        return ret;
    }

}

export class BarUnit {

    public definitionName: string = "";

    public health: number = 0;
    public metalCost: number = 0;
    public energyCost: number = 0;
    public buildTime: number = 0;
    public speed: number = 0;
    public turnRate: number = 0;
    public energyProduced: number = 0;
    public windGenerator: number = 0;
    public energyStorage: number = 0;
    public energyUpkeep: number = 0;
    public extractsMetal: boolean = false;
    public metalExtractor: number = 0;
    public metalProduced: number = 0;
    public metalStorage: number = 0;

    // builder stuff
    public buildDistance: number = 0;
    public buildPower: number = 0;
    public isBuilder: boolean = false;
    public canAssist: boolean = false;
    public canReclaim: boolean = false;
    public canRepair: boolean = false;
    public canRestore: boolean = false;
    public canResurrect: boolean = false;

    // los
    public sightDistance: number = 0;
    public airSightDistance: number = 0;
    public radarDistance: number = 0;
    public sonarDistance: number = 0;
    public jamDistance: number = 0;
    public transportCapacity: number = 0;
    public transportMass: number = 0;
    public transportSize: number = 0;

    // misc
    public modelAuthor: string | null = null;
    public cloakCostStill: number = 0;
    public cloakCostMoving: number = 0;
    public explodeAs: string = "";
    public selfDestructWeapon: string = "";
    public selfDestructCountdown: number = 0;
    public isStealth: boolean = false;
    public paralyzeMultiplier: number = 0;

    public weapons: BarUnitWeapon[] = [];

    public selfDestructWeaponDefinition: BarWeaponDefinition = new BarWeaponDefinition();
    public selfDestructDamage: number = 0;
    public explodeAsWeapon: BarWeaponDefinition = new BarWeaponDefinition();

    public static parse(elem: any): BarUnit {
        return {
            ...elem,
            weapons: elem.weapons.map((iter: any) => BarUnitWeapon.parse(iter))
        };
    }

}

export class BarWeaponDefinition {

    public definitionName: string = "";

    public name: string = "";

    public areaOfEffect: number = 0;

    public burst: number = 0;

    public burstRate: number = 0;

    public edgeEffectiveness: number = 0;

    public flightTime: number = 0;

    public impulseFactor: number = 0;

    public impactOnly: boolean = false;

    public range: number = 0;

    public reloadTime: number = 0;


    public turnRate: number = 0;

    public velocity: number = 0;

    public weaponType: string = "";

    public tracks: boolean = false;

    public waterWeapon: boolean = false;

    public energyPerShot: number = 0;

    public metalPerShot: number = 0;

    public isStockpile: boolean = false;

    public stockpileTime: number = 0;

    public stockpileLimit: number = 0;

    public isParalyzer: boolean = false;

    public paralyzerTime: number = 0;

    public paralyzerExceptions: string = "";

    public isBogus: boolean = false;

    public damages: Map<string, number> = new Map();

    public shieldData: BarShieldData | null = null;

    public carriedUnit: BarUnitCarrierData | null = null;

    // computed props

    public fireRate: number = 0;

    public defaultDamage: number = 0;

    public defaultBurstDamage: number = 0;

    public defaultDps: number = 0;

    public static parse(elem: any): BarWeaponDefinition {
        const def: BarWeaponDefinition = {
            ...elem,
            shieldData: (elem.shieldData == null) ? null : BarShieldData.parse(elem.shieldData),
            carriedUnit: (elem.carriedUnit == null) ? null : BarUnitCarrierData.parse(elem.carriedUnit)
        };

        def.damages = new Map<string, number>();
        for (const key of Object.keys(elem.damages)) {
            def.damages.set(key, elem.damages[key]);
        }

        def.fireRate = 1 / Math.max(0.0001, def.reloadTime);

        def.defaultDamage = def.damages.get("default") ?? def.damages.get("vtol") ?? 0;
        def.defaultBurstDamage = def.defaultDamage * Math.max(1, def.burst);
        def.defaultDps =def.defaultDamage / Math.max(0.01, def.reloadTime);
        if (def.burst != 0) {
            def.defaultDps *= def.burst;
        }

        return def;
    }

}

export class BarUnitWeapon {

    public weaponDefinition: BarWeaponDefinition = new BarWeaponDefinition();

    public count: number = 0;

    public targetCategory: string = "";

    public static parse(elem: any): BarUnitWeapon {
        return {
            weaponDefinition: BarWeaponDefinition.parse(elem.weaponDefinition),
            count: elem.count,
            targetCategory: elem.targetCategory
        };
    }

}

export class BarShieldData {

    public energyUpkeep: number = 0;

    public power: number = 0;

    public powerRegen: number = 0;

    public powerRegenEnergy: number = 0;

    public radius: number = 0;

    public repulser: boolean = false;

    public startingPower: number = 0;

    public force: number = 0;

    public static parse(elem: any): BarShieldData {
        return { ...elem };
    }

}

export class BarUnitCarrierData {

    public definitionName: string = "";

    public engagementRange: number = 0;

    public spawnSurface: string = "";

    public spawnRate: number = 0;

    public maxUnits: number = 0;

    public energyCost: number = 0;

    public metalCost: number = 0;

    public controlRadius: number = 0;

    public decayRate: number = 0;

    public enableDocking: boolean = false;

    public dockingArmor: number = 0;

    public dockingHealRate: number = 0;

    public dockToHealThreshold: number = 0;

    public dockingHelperSpeed: number = 0;

    public static parse(elem: any): BarUnitCarrierData {
        return {
            ...elem
        };
    }

}