import { GamemodeUtil } from "util/Gamemode";
import Vue from "vue";

Vue.filter("gamemode", (id: number): string => {
    return GamemodeUtil.getName(id);
});