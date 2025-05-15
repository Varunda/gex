import { FactionUtil } from "util/Faction";
import Vue from "vue";

Vue.filter("faction", (id: number): string => {
    return FactionUtil.getName(id);
});