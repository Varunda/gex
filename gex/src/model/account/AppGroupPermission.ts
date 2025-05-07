
export class AppGroupPermission {
    public id: number = 0;
    public groupID: number = 0;
    public permission: string = "";
    public timestamp: Date = new Date();
    public grantedByID: number = 0;

    public static parse(elem: any): AppGroupPermission {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }

}
