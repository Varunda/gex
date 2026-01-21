import Vue from "vue";

Vue.filter("compactUnitName", (displayName: string) => {
    if (displayName == "Wind Turbine") {
        return "Wind";
    }

    if (displayName == "Metal Extractor") {
        return "Mex";
    }

    if (displayName == "Solar Collector") {
        return "Solar";
    }

    return displayName;
});