import { BarMap } from "./BarMap";
import { BarMatchAllyTeam } from "./BarMatchAllyTeam";
import { BarMatchChatMessage } from "./BarMatchChatMessage";
import { BarMatchPlayer } from "./BarMatchPlayer";
import { BarMatchProcessing } from "./BarMatchProcessing";
import { BarMatchSpectator } from "./BarMatchSpectator";

export class BarMatch {
    public id: string = "";
    public engine: string = "";
    public gameVersion: string = "";
    public startTime: Date = new Date();
    public map: string = "";
    public mapName: string = "";
    public fileName: string = "";
    public durationMs: number = 0;

    public hostSettings: any = {};
    public gameSettings: any = {};
    public mapSettings: any = {};
    public spadsSettings: any = {};

    public allyTeams: BarMatchAllyTeam[] = [];
    public players: BarMatchPlayer[] = [];
    public spectators: BarMatchSpectator[] = [];
    public chatMessages: BarMatchChatMessage[] = [];

    public mapData: BarMap | null = null;
    public processing: BarMatchProcessing | null = null;

    public static parse(elem: any): BarMatch {
        return {
            ...elem,
            startTime: new Date(elem.startTime),

            hostSettings: elem.hostSettings,
            gameSettings: elem.gameSettings,
            mapSettings: elem.mapSettings,
            spadsSettings: elem.spadsSettings,

            allyTeams: (elem.allyTeams.map((iter: any) => BarMatchAllyTeam.parse(iter)) as BarMatchAllyTeam[]).sort((a, b) => a.allyTeamID - b.allyTeamID),
            players: (elem.players.map((iter: any) => BarMatchPlayer.parse(iter)) as BarMatchPlayer[]).sort((a, b) => a.playerID - b.playerID),
            spectators: (elem.spectators.map((iter: any) => BarMatchSpectator.parse(iter)) as BarMatchSpectator[]).sort((a, b) => a.username.localeCompare(b.username)),
            chatMessages: elem.chatMessages.map((iter: any) => BarMatchChatMessage.parse(iter)),

            mapData: elem.mapData == null ? null : BarMap.parse(elem.mapData),
            processing: elem.processing == null ? null : BarMatchProcessing.parse(elem.processing)
        }
    }

}