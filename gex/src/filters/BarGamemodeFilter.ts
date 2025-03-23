import Vue from "vue";

Vue.filter("gamemode", (id: number): string => {
    if (id == 0) {
        return "Unknown";
    } else if (id == 1) {
        return "Duel";
    } else if (id == 2) {
        return "Small team";
    } else if (id == 3) {
        return "Large team";
    } else if (id == 4) {
        return "FFA";
    }
    return `<unchecked gamemode ${id}>`;
});