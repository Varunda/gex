
export class AppAccount {
    public id: number = 0;
    public name: string = "";
    public discordID: string = "0";
    public deletedOn: Date | null = null;
    public deletedBy: number | null = null;

    public static parse(elem: any): AppAccount {
        return {
            ...elem,
            discordID: elem.discordID.toString(),
            deletedOn: elem.deletedOn == null ? null : new Date(elem.deletedOn)
        };
    }
}