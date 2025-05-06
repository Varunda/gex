
import { Loading } from "Loading";
import ApiWrapper from "api/ApiWrapper";
import { BarMatch } from "model/BarMatch";

export class MatchUploadApi extends ApiWrapper<BarMatch> {
    private static _instance: MatchUploadApi = new MatchUploadApi();
    public static get(): MatchUploadApi { return MatchUploadApi._instance; }

    public static upload(file: File): Promise<Loading<BarMatch>> {
        const formData: FormData = new FormData();
        formData.append("data", file);

        return MatchUploadApi.get().postReplyForm("/api/match-upload/upload", formData, BarMatch.parse);
    }


}