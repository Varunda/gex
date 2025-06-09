import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { SkillHistogramEntry } from "model/SkillHistogramEntry";

export class SkillHistogramApi extends ApiWrapper<SkillHistogramEntry> {
    private static _instance: SkillHistogramApi = new SkillHistogramApi();
    public static get(): SkillHistogramApi { return SkillHistogramApi._instance; }

    public static getHistogram(): Promise<Loading<SkillHistogramEntry[]>> {
        return SkillHistogramApi.get().readList(`/api/skill-histogram`, SkillHistogramEntry.parse);
    }

}