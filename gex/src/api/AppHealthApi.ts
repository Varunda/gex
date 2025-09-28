import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { HeadlessRunStatus } from "model/HeadlessRunStatus";

export class AppHealth {
    public timestamp: Date = new Date();
    public queues: ServiceQueueCount[] = [];
    public services: AppService[] = [];
    public headlessRuns: HeadlessRunStatus[] = [];
}

export class AppService {
    public name: string = "";
    public lastRan: Date = new Date();
    public runDuration: number = 0;
    public enabled: boolean = true;
    public message: string = "";
}

export class ServiceQueueCount {
    public queueName: string = "";
    public typeName: string = "";
    public count: number = 0;
    public processed: number = 0;
    public average: number | null = null;
    public min: number | null = null;
    public max: number | null = null;
    public median: number | null = null;
}

export class AppHealthApi extends ApiWrapper<AppHealth> {
    private static _instance: AppHealthApi = new AppHealthApi();
    public static get(): AppHealthApi { return AppHealthApi._instance; }

    public static parseQueue(elem: any): ServiceQueueCount {
        return {
            ...elem
        };
    }

    public static parseService(elem: any): AppService {
        return {
            ...elem,
            lastRan: new Date(elem.lastRan)
        };
    }

    public static parse(elem: any): AppHealth {
        return {
            queues: elem.queues.map((iter: any) => AppHealthApi.parseQueue(iter)),
            services: elem.services.map((iter: any) => AppHealthApi.parseService(iter)),
            headlessRuns: elem.headlessRuns.map((iter: any) => HeadlessRunStatus.parse(iter)),
            timestamp: new Date(elem.timestamp)
        };
    }

    public static async getHealth(): Promise<Loading<AppHealth>> {
        return AppHealthApi.get().readSingle(`/api/health`, AppHealthApi.parse);
    }

    public static async disableService(name: string): Promise<Loading<void>> {
        return AppHealthApi.get().post(`/api/health/disable/${encodeURIComponent(name)}`);
    }

    public static async enableService(name: string): Promise<Loading<void>> {
        return AppHealthApi.get().post(`/api/health/enable/${encodeURIComponent(name)}`);
    }

}
