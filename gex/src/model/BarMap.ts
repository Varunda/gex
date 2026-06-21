
export class BarMap {
    public id: number = 0;
    public name: string = "";
    public fileName: string = "";
    public description: string = "";
    public tidalStrength: number = 0;
    public maxMetal: number = 0;
    public extractorRadius: number = 0;
    public minimumWind: number = 0;
    public maximumWind: number = 0;
    public width: number = 0;
    public height: number = 0;
    public author: string = "";
    public symmetryAxis: number = 0;

    public startPositionData: StartPositionData | null = null;

    public static parse(elem: any): BarMap {
        return {
            ...elem,
            startPositionData: (elem.startPositionData == null) ? null : StartPositionData.parse(elem.startPositionData)
        };
    }

}

export class StartPositionData {

    public mapFilename: string = "";
    public version: number = 0;
    public timestamp: Date = new Date();
    public minTimestamp: Date = new Date();
    public maxTimestamp: Date | null = null;
    public positions: StartPosition[] = [];
    public configurations: StartTeamConfiguration[] = [];

    public static parse(elem: any): StartPositionData {
        return {
            ...elem,
            positions: elem.positions.map((iter: any) => StartPosition.parse(iter)),
            configurations: elem.configurations.map((iter: any) => StartTeamConfiguration.parse(iter)),
            timestamp: new Date(elem.timestamp)
        };
    }

}

export class StartPosition {
    public name: string = "";
    public x: number = 0;
    public y: number = 0;
    public maxRadius: number = 0;

    public static parse(elem: any): StartPosition {
        return {
            ...elem
        }
    }
}

export class StartTeamConfiguration {
    public playersPerTeam: number = 0;
    public sides: StartTeamSide[] = [];
    public teamCount: number = 0;

    public static parse(elem: any): StartTeamConfiguration {
        return {
            playersPerTeam: elem.playersPerTeam,
            sides: elem.sides.map((iter: any) => StartTeamSide.parse(iter)),
            teamCount: elem.teamCount
        };
    }
}

export class StartTeamSide {
    public starts: StartTeamSideStart[] = [];

    public static parse(elem: any): StartTeamSide {
        return {
            starts: elem.starts.map((iter: any) => StartTeamSideStart.parse(iter))
        }
    }
}

export class StartTeamSideStart {
    public mapFilename: string = "";
    public version: number = 0;
    public role: string = "";
    public spawnPoint: string = "";
    public baseCenter: string | null = null;

    public static parse(elem: any): StartTeamSideStart {
        let role: string = elem.role;
        role = role.replaceAll("front", "Front").replaceAll("air", "Air").replaceAll("tech", "Tech").replaceAll("sea", "Sea");

        return {
            mapFilename: elem.mapFilename,
            version: elem.version,
            role: role,
            spawnPoint: elem.spawnPoint,
            baseCenter: elem.baseCenter == undefined ? null : elem.baseCenter
        };
    }
}