
export class AppAccountGroupMembership {
    public id: number = 0;
    public accountID: number = 0;
    public groupID: number = 0;
    public timestamp: Date = new Date();
    public grantedByAccountID: number = 0;

    public static parse(elem: any): AppAccountGroupMembership {
        return {
            ...elem,
            timestamp: new Date(elem.timestamp)
        };
    }
}
