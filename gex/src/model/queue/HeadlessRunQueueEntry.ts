
export class HeadlessRunQueueEntry {
    public gameID: string = "";
    public force: boolean = false;
    public forceForward: boolean = false;

    public static parse(elem: any): HeadlessRunQueueEntry {
        return {
            ...elem
        };
    }

}