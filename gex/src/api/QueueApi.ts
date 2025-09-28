
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { HeadlessRunQueueEntry } from "model/queue/HeadlessRunQueueEntry";

export class QueueApi extends ApiWrapper<HeadlessRunQueueEntry> {
    private static _instance: QueueApi = new QueueApi();
    public static get(): QueueApi { return QueueApi._instance; }

    public static getHeadlessQueue(): Promise<Loading<HeadlessRunQueueEntry[]>> {
        return QueueApi.get().readList(`/api/queue/headless`, HeadlessRunQueueEntry.parse);
    }

    public static clearQueue(name: string): Promise<Loading<void>> {
        return QueueApi.get().post(`/api/queue/clear/${name}`);
    }

}