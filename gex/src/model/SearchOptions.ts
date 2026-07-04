import { SearchKeyValue } from "model/BarMatch";

export class SearchOptions {
    public engine?: string;
    public gameVersion?: string;
    public map?: string;
    public startTimeAfter?: Date;
    public startTimeBefore?: Date;

    public durationMinimum?: number;
    public durationMaximum?: number;
    public ranked?: boolean;
    public gamemode?: number;

    public playerCountMinimum?: number;
    public playerCountMaximum?: number;
    public legionEnabled?: boolean;
    public poolID?: number;

    public gameSettings?: SearchKeyValue[];
    public users?: { username: string; userID: number }[];
    public players?: SearchPlayer[];

    public minOS?: number;
    public maxOS?: number;
    public minAvgOS?: number;
    public maxAvgOS?: number;

    public processingDownloaded?: boolean;
    public processingParsed?: boolean;
    public processingReplayed?: boolean;
    public processingAction?: boolean;
}

export class SearchPlayer {
    public userID?: number;
    public positionLabel?: string;
    public minOS?: number;
    public maxOS?: number;
}