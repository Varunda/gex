import ApiWrapper from "./ApiWrapper";
import { Loading } from "Loading";

import { MatchProcessingWebhook } from "model/MatchProcessingWebhook";

export class MatchProcessingWebhookApi extends ApiWrapper<MatchProcessingWebhook> {
    private static _instance: MatchProcessingWebhookApi = new MatchProcessingWebhookApi();
    public static get(): MatchProcessingWebhookApi { return MatchProcessingWebhookApi._instance; }

    public static getByCurrentUser(): Promise<Loading<MatchProcessingWebhook[]>> {
        return MatchProcessingWebhookApi.get().readList(`/api/match-processing-webhook`, MatchProcessingWebhook.parse);
    }

    public static create(url: string, type: string, includeEvents: boolean, sharedSecret: string): Promise<Loading<void>> {
        const params: URLSearchParams = new URLSearchParams();
        params.set("url", url);
        params.set("type", type);
        params.set("includeEvents", includeEvents ? "true" : "false");
        params.set("sharedSecret", sharedSecret);

        return MatchProcessingWebhookApi.get().post(`/api/match-processing-webhook?${params.toString()}`);
    }

    public static sendTest(url: string, type: string, includeEvents: boolean, sharedSecret: string): Promise<Loading<void>> {
        const params: URLSearchParams = new URLSearchParams();
        params.set("url", url);
        params.set("type", type);
        params.set("includeEvents", includeEvents ? "true" : "false");
        params.set("sharedSecret", sharedSecret);

        return MatchProcessingWebhookApi.get().post(`/api/match-processing-webhook/test?${params.toString()}`);
    }

    public static deleteWebhook(url: string, type: string, sharedSecret: string): Promise<Loading<void>> {
        const params: URLSearchParams = new URLSearchParams();
        params.set("url", url);
        params.set("type", type);
        params.set("sharedSecret", sharedSecret);

        return MatchProcessingWebhookApi.get().delete(`/api/match-processing-webhook?${params.toString()}`);
    }

}