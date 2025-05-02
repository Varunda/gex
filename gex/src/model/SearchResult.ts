
export class SearchResult {
    public value: string = "";

    public static parse(elem: any): SearchResult {
        return {
            value: elem.value
        };
    }

}