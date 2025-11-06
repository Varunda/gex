import { AppAccount } from "./account/AppAccount";
import { ApmStats } from "./ApmStats";
import { BarMap } from "./BarMap";
import { BarMatchAllyTeam } from "./BarMatchAllyTeam";
import { BarMatchChatMessage } from "./BarMatchChatMessage";
import { BarMatchPlayer } from "./BarMatchPlayer";
import { BarMatchProcessing } from "./BarMatchProcessing";
import { BarMatchSpectator } from "./BarMatchSpectator";
import { BarMatchTeamDeath } from "./BarMatchTeamDeath";
import { HeadlessRunStatus } from "./HeadlessRunStatus";

export class BarMatch {
    public id: string = "";
    public engine: string = "";
    public gameVersion: string = "";
    public startTime: Date = new Date();
    public startOffset: number = 0;
    public endTime: Date = new Date();
    public map: string = "";
    public mapName: string = "";
    public fileName: string = "";
    public durationMs: number = 0;
    public uploadedByID: number | null = null;
    public wrongSkillValues: boolean = false;
    public gamemode: number = 0;

    public hostSettings: any = {};
    public gameSettings: any = {};
    public mapSettings: any = {};
    public spadsSettings: any = {};

    public allyTeams: BarMatchAllyTeam[] = [];
    public players: BarMatchPlayer[] = [];
    public spectators: BarMatchSpectator[] = [];
    public chatMessages: BarMatchChatMessage[] = [];
    public teamDeaths: BarMatchTeamDeath[] = [];

    public mapData: BarMap | null = null;
    public processing: BarMatchProcessing | null = null;
    public usersPrioritizing: string[] = [];
    public headlessRunStatus: HeadlessRunStatus | null = null;
    public uploadedBy: AppAccount | null = null;

    public static parse(elem: any): BarMatch {
        return {
            ...elem,
            startTime: new Date(elem.startTime),
            startOffset: elem.startOffset,
            endTime: new Date((new Date(elem.startTime)).getTime() + elem.durationMs),

            hostSettings: elem.hostSettings,
            gameSettings: elem.gameSettings,
            mapSettings: elem.mapSettings,
            spadsSettings: elem.spadsSettings,

            allyTeams: (elem.allyTeams.map((iter: any) => BarMatchAllyTeam.parse(iter)) as BarMatchAllyTeam[]).sort((a, b) => a.allyTeamID - b.allyTeamID),
            players: (elem.players.map((iter: any) => BarMatchPlayer.parse(iter)) as BarMatchPlayer[]).sort((a, b) => a.playerID - b.playerID),
            spectators: (elem.spectators.map((iter: any) => BarMatchSpectator.parse(iter)) as BarMatchSpectator[]).sort((a, b) => a.username.localeCompare(b.username)),
            chatMessages: elem.chatMessages.map((iter: any) => BarMatchChatMessage.parse(iter)),
            teamDeaths: elem.teamDeaths.map((iter: any) => BarMatchTeamDeath.parse(iter)),

            mapData: elem.mapData == null ? null : BarMap.parse(elem.mapData),
            processing: elem.processing == null ? null : BarMatchProcessing.parse(elem.processing),
            usersPrioritizing: elem.usersPrioritizing,
            headlessRunStatus: elem.headlessRunStatus == null ? null : HeadlessRunStatus.parse(elem.headlessRunStatus),
            uploadedBy: !elem.uploadedBy ? null : AppAccount.parse(elem.uploadedBy)
        }
    }

}

export class SearchKeyValue {
    public key: string = "";
    public value: string = "";
    public operation: "eq" | "ne" = "eq";
}