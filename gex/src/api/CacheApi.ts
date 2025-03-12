import { Loading, Loadable } from "Loading";
import ApiWrapper from "api/ApiWrapper";

export class CacheEntryMetadata {
    public key: string = "";
    public created: Date = new Date();
    public lastAccessed: Date = new Date();
    public uses: number = 0;
}

export class CacheApi extends ApiWrapper<CacheEntryMetadata> {
    private static _instance: CacheApi = new CacheApi();
    public static get(): CacheApi { return CacheApi._instance; }

    public static parse(elem: any): CacheEntryMetadata {
        return {
            key: elem.key,
            created: new Date(elem.created),
            lastAccessed: new Date(elem.lastAccessed),
            uses: elem.uses
        };
    }

    public static async getKeys(): Promise<Loading<string[]>> {
        return CacheApi.get().readList(`/api/cache/`, (elem: any) => elem);
    }

    public static async getValue(key: string): Promise<Loading<string>> {
        return CacheApi.get().readSingle(`/api/cache/${key}`, (elem: any) => elem);
    }

    public static getMetadata(key: string): Promise<Loading<CacheEntryMetadata>> {
        return CacheApi.get().readSingle(`/api/cache/${key}/meta`, CacheApi.parse);
    }

    public static evict(key: string): Promise<Loading<void>> {
        return CacheApi.get().delete(`/api/cache/${key}`);
    }

}
