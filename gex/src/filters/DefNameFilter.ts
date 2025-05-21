import { DefNameUtil } from "util/DefName";
import Vue from "vue";

Vue.filter("defName", (defName: string): string => {
    return DefNameUtil.getName(defName);
});