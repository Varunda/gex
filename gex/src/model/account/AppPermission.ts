
export class AppPermission {
    public id: string = "";
    public description: string = "";

    public static parse(elem: any): AppPermission {
        return {
            ...elem,
        };
    }
}