import Vue from "vue";
import * as luxon from "luxon";

Vue.filter("compactTimeAgo", (date: Date | number): string => {
	if (typeof date == "number") {
		date = new Date(date);
    }

    const m = luxon.DateTime.fromJSDate(date);
    const now = luxon.DateTime.fromJSDate(new Date(Date.now()));

	const years = now.diff(m, "years").as("years");
	const months = now.diff(m, "months").as("months") % 12;

	if (years >= 1) {
		return `${years}Y ${months}M`;
	}

	const days = Math.floor(now.diff(m, "days").as("days"));
	if (months >= 1) {
		return `${months}M ${days % 30}d`;
	}

	const hours = Math.floor(now.diff(m, "hours").as("hours")) % 24;
	if (days >= 1) {
		return `${days}d ${hours}h`;
	}

	const mins = Math.floor(now.diff(m, "minutes").as("minutes")) % 60;
	if (hours >= 1) {
        return `${hours} hour${hours == 1 ? "" : "s"}`;
    }

	return `${mins} minutes`;
});
