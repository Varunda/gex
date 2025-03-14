

export class BarMatchSpectator {
    public gameID: string = "";
    public userID: number = 0;
    public username: string = "";

    public static parse(elem: any): BarMatchSpectator {
        return {
            gameID: elem.gameID,
            userID: elem.userID,
            username: elem.name
        };
    }
}