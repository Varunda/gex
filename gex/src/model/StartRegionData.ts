
export class StartRegionData {

    public allyTeamID: number = 0;
    public regions: StartRegion[] = [];

    public static parse(elem: any): StartRegionData {
        return {
            allyTeamID: elem.allyTeamID,
            regions: elem.regions.map((iter: any) => StartRegion.parse(iter))
        };
    }

}

export class StartRegion {

    public vertices: StartRegionVertex[] = [];

    public static parse(elem: any): StartRegion {
        return {
            vertices: elem.vertices.map((iter: any) => StartRegionVertex.parse(iter))
        };
    }

}

export class StartRegionVertex {
    public x: number = 0;
    public z: number = 0;

    public static parse(elem: any): StartRegionVertex {
        return {
            x: elem.x,
            z: elem.z
        };
    }

}