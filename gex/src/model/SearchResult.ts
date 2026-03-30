
export class SearchResult {
    public value: string = "";
    public name: string = "";

    public static parse(elem: any): SearchResult {
        return {
            value: elem.value,
            name: (elem.name == undefined) ? elem.value : elem.name
        };
    }

}