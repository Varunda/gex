

export class BarMatchSpectator {
    public gameID: string = "";
    public playerID: number = 0;
    public userID: number = 0;
    public username: string = "";

    public static parse(elem: any): BarMatchSpectator {
        return {
            gameID: elem.gameID,
            playerID: elem.playerID,
            userID: elem.userID,
            username: elem.name
        };
    }
}