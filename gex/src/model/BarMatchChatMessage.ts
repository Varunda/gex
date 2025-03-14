
export class BarMatchChatMessage {
    public gameID: string = "";
    public gameTimestamp: number = 0;
    public fromId: number = 0;
    public toId: number = 0;
    public message: string = "";

    public static parse(elem: any): BarMatchChatMessage {
        return {
            ...elem
        };
    }

}