
export class MatchProcessingWebhook {

    public url: string = "";
    public type: string = "";
    public sharedSecret: string = "";
    public includeEvents: boolean = true;
    public timestamp: Date = new Date();
    public userID: number = 0;

    public static parse(elem: any): MatchProcessingWebhook {
        return {
            ...elem
        }
    }

}