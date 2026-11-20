import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";

import { BarTweakEntry } from "model/BarTweakEntry";

export class BarTweakEntryApi extends ApiWrapper<BarTweakEntry> {
    private static _instance: BarTweakEntryApi = new BarTweakEntryApi();
    public static get(): BarTweakEntryApi { return BarTweakEntryApi._instance; }

    public static getAll(): Promise<Loading<BarTweakEntry[]>> {
        return BarTweakEntryApi.get().readList(`/api/tweak-entry/all`, BarTweakEntry.parse);
    }

}