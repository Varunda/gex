import { BarMatch } from "./BarMatch";

export class ApiMatchSearchResults {

    public query: string = "";
    public normalized: string = "";
    public tokens: SearchToken[] = [];
    public ast: SearchNode = new SearchNode();
    public results: BarMatch[] = [];
    public found: number = 0;

    public static parse(elem: any): ApiMatchSearchResults {
        return {
            query: elem.query,
            normalized: elem.normalized,
            tokens: elem.tokens.map((iter: any) => SearchToken.parse(iter)),
            ast: SearchNode.parse(elem.ast.root),
            results: elem.results.map((iter: any) => BarMatch.parse(iter)),
            found: elem.found
        };
    }

}

export class SearchNode {
    public type: string = "";
    public token: SearchToken = new SearchToken();
    public children: SearchNode[] = [];
    public depth: number = 0;

    public static parse(elem: any): SearchNode {
        return {
            type: elem.type,
            token: SearchToken.parse(elem.token),
            children: elem.children.map((iter: any) => SearchNode.parse(iter)),
            depth: elem.depth
        };
    }

}

export class SearchToken {
    public type: string = "";
    public value: string = "";

    public static parse(elem: any): SearchToken {
        return {
            type: elem.type,
            value: elem.value
        };
    }

}