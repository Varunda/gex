
export class BarMatchProcessing {
    public gameID: string = "";
    public replayDownloaded: Date | null = null;
    public replayParsed: Date | null = null;
    public replaySimulated: Date | null = null;
    public actionsParsed: Date | null = null;
    public replayDownloadedMs: number | null = null;
    public replayParsedMs: number | null = null;
    public replaySimulatedMs: number | null = null;
    public actionsParsedMs: number | null = null;

    public static parse(elem: any): BarMatchProcessing {
        return {
            gameID: elem.gameID,
            replayDownloaded: (elem.replayDownloaded == null) ? null : new Date(elem.replayDownloaded),
            replayParsed: (elem.replayParsed == null) ? null : new Date(elem.replayParsed),
            replaySimulated: (elem.replaySimulated == null) ? null : new Date(elem.replaySimulated),
            actionsParsed: (elem.actionsParsed == null) ? null : new Date(elem.actionsParsed),
            replayDownloadedMs: elem.replayDownloadedMs,
            replayParsedMs: elem.replayParsedMs,
            replaySimulatedMs: elem.replaySimulatedMs,
            actionsParsedMs: elem.actionsParsedMs
        };
    }

}
