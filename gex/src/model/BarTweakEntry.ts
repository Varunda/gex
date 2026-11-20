import Base64Util from "util/Base64";

import { md5 } from "js-md5";

export class BarTweakEntry {
    public hash: string = "";
    public day: Date = new Date();
    public name: string = "";
    public fullName: string = "";
    public base64: string = "";
    public code: string = "";
    public count: number = 0;
    public lobbyHash: string = "";

    public static parse(elem: any): BarTweakEntry {
        const h = md5.create();
        h.update(elem.base64);

        return {
            ...elem,
            code: Base64Util.decode(elem.base64),
            lobbyHash: btoa(h.hex()),
            fullName: elem.name + btoa(h.hex().toLowerCase()),
        };
    }

}