
export class HeadlessRunStatus {
    public gameID: string = "";
    public simulating: boolean = false;
    public frame: number = 0;
    public durationFrames: number = 0;
    public timestamp: Date = new Date();
    public fps: number = 0;

    public static parse(elem: any): HeadlessRunStatus {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        }
    }
}