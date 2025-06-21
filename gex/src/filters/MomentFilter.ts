import Vue from "vue";

import * as luxon from "luxon";

function vueLuxon(input: Date | string | number | null | undefined, format: string = "yyyy-MM-dd hh:mma ZZZZ"): string {
    if (input == "" || input == undefined || input == null) {
        return "";
    }

    let dt: luxon.DateTime;
    if (input instanceof Date) {
        dt = luxon.DateTime.fromJSDate(input);
    } else if (typeof input == "string") {
        dt = luxon.DateTime.fromJSDate(new Date(input));
    } else if (typeof input == "number") {
        dt = luxon.DateTime.fromJSDate(new Date(input));
    } else {
        throw `unchecked type of input`;
    }

    return dt.toFormat(format);
}

Vue.filter("moment", vueLuxon);

Vue.filter("duration", (input: string | number, format: string): string => {
    const val = (typeof(input) == "string") ? Number.parseInt(input) : input;
    if (Number.isNaN(val)) {
        return `NaN ${val}`;
    }

    let isPast: boolean = false;
    let str: string = "";

    if (val < 0) {
        isPast = true;
    }

    if (val == 0) {
        return "Never";
    }

    const parts = {
        seconds: 0 as number,
        minutes: 0 as number,
        hour: 0 as number
    };

    if (val == 1) {
        parts.seconds = 1;
        str = "1s";
    } else if (val < 60) {
        parts.seconds = val % 60;
        str = `${val % 60}s`;
    } else if (val == 0) {
        parts.minutes = 1;
        str = `00:01`;
    } else if (val < (60 * 60)) {
        parts.minutes = Math.round(val / 60);
        parts.seconds = val % 60;
        str = `00:${Math.round(val / 60).toString().padStart(2, "0")}`;
    } else if (val == 60 * 60) {
        parts.hour = 1;
        str = `01:00`;
    }

    const hours = Math.floor(val / 3600);
    const mins = Math.floor((val - (3600 * hours)) / 60);
    const secs = val % 60;

    parts.hour = hours;
    parts.minutes = mins;
    parts.seconds = secs;

    str = `${hours.toString().padStart(2, "0")}:${mins.toString().padStart(2, "0")}`;

    return `${str}${(isPast == true ? " ago" : "")}`;
});

Vue.filter("mduration", (input: string | number): string => {
    const val: number = (typeof(input) == "string") ? Number.parseInt(input) : input;
    if (Number.isNaN(val)) {
        return `NaN ${val}`;
    }

    const dur: luxon.Duration = luxon.Duration.fromObject({ seconds: val });

    if (dur.as("days") >= 1) {
        return `${Math.floor(dur.as("days"))}d ${(Math.floor(dur.as("hours")) % 24).toString().padStart(2, "0")}h`;
    }

    if (dur.as("hours") >= 1) {
        return `${dur.as("hours")}h ${(dur.as("minutes") % 60).toString().padStart(2, "0")}m`;
    }

    if (dur.as("minutes") >= 1) {
        return `${Math.floor(dur.as("minutes")).toString().padStart(2, "0")}m ${Math.floor(dur.as("seconds") % 60).toString().padStart(2, "0")}s`;
    }

    return `00m ${Math.floor(dur.as("seconds")).toString().padStart(2, "0")}s`;
});
