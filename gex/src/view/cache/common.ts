
export class CacheEntry {
    public key: string = "";
    public parent: CacheEntry | null = null;
    public children: Map<string, CacheEntry> = new Map();
    public depth: number = 0;
    public treeCount: number = 0;
}