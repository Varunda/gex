
export class MapUtil {

    public static getNameNameWithoutVersion(name: string): string {
        name = name.replace(/ /g, " ");
        const m = name.match(/^([a-zA-Z\-_\d\s']*)[vV_\s][\d\.]*/);
        if (m == null) {
            return name;
        }
        if (m.length < 2) {
            return name;
        }
        return m[1].replace(/_/g, " ");
    }

}