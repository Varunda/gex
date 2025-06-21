
export class UserPreviousName {

    public userName: string = "";

    public timestamp: Date = new Date();

    public static parse(elem: any): UserPreviousName {
        return {
            userName: elem.userName,
            timestamp: new Date(elem.timestamp)
        };
    }

}