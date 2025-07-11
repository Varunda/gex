import * as luxon from "luxon";

export default class TimeUtils {

    public static duration(seconds: number): string {
        const dur: luxon.Duration = luxon.Duration.fromObject({ seconds: seconds });

        if (dur.as("days") >= 1) {
            return `${Math.floor(dur.as("days"))}d ${(Math.floor(dur.as("hours")) % 24).toString().padStart(2, "0")}h`;
        }

        if (dur.as("hours") >= 1) {
            return `${Math.floor(dur.as("hours"))}h ${(Math.floor(dur.as("minutes") % 60)).toString().padStart(2, "0")}m`;
        }

        if (dur.as("minutes") >= 1) {
            return `${Math.floor(dur.as("minutes")).toString().padStart(2, "0")}m ${Math.floor(dur.as("seconds") % 60).toString().padStart(2, "0")}s`;
        }

        return `00m ${Math.floor(dur.as("seconds")).toString().padStart(2, "0")}s`;
    }

    public static format(date: Date, format: string = "yyyy-MM-dd hh:mma ZZZZ"): string {
        return luxon.DateTime.fromJSDate(date).toFormat(format);
    }

    public static formatNoTimezone(date: Date, format: string = "yyyy-MM-dd hh:mmz"): string {
        return luxon.DateTime.fromJSDate(date).toFormat(format);
    }

}
