import Base64Util from "util/Base64";

import { sha512 } from "js-sha512";

export class BarTweakEntry {
    public hash: string = "";
    public day: Date = new Date();
    public type: string = "";

    public name: string = "";
    public base64: string = "";
    public count: number = 0;

    // generated values
    public code: string = "";
    public lobbyHash: string = "";
    public fullName: string = "";

    public static parse(elem: any): BarTweakEntry {
        const h = sha512.create();
        h.update(elem.base64);

        return {
            ...elem,

            code: Base64Util.decode(elem.base64),
            lobbyHash: btoa(h.hex()),
            fullName: elem.name + " = " + elem.base64.length + ":" + btoa(h.hex()).substring(0, 4),
        };
    }

}