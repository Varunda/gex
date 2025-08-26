
export class BarUserSkillChanges {
    public userID: number = 0;
    public gamemodes: BarUserSkillGamemode[] = [];

    public static parse(elem: any): BarUserSkillChanges {
        return {
            userID: elem.userID,
            gamemodes: elem.gamemodes.map((iter: any) => BarUserSkillGamemode.parse(iter))
        };
    }
}

export class BarUserSkillGamemode {
    public gamemode: number = 0;
    public changes: BarUserSkillChangeEntry[] = [];

    public static parse(elem: any): BarUserSkillGamemode {
        return {
            gamemode: elem.gamemode,
            changes: elem.changes.map((iter: any) => BarUserSkillChangeEntry.parse(iter))
        };
    }
}

export class BarUserSkillChangeEntry {
    public skill: number = 0;
    public skillUncertainty: number = 0;
    public timestamp: Date = new Date();

    public static parse(elem: any): BarUserSkillChangeEntry {
        return {
            skill: elem.skill,
            skillUncertainty: elem.skillUncertainty,
            timestamp: new Date(elem.timestamp)
        };
    }
}