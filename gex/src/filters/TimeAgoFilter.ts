import Vue from "vue";
import * as luxon from "luxon";

Vue.filter("timeAgo", (date: Date | number): string => {
	if (typeof date == "number") {
		date = new Date(date);
    }

    const m = luxon.DateTime.fromJSDate(date);
    const now = luxon.DateTime.fromJSDate(new Date(Date.now()));

	const years = now.diff(m, "years").as("years");
	const months = now.diff(m, "months").as("months") % 12;

	if (years >= 1) {
		return `${years.toFixed(0)}Y ${months.toFixed(0)}M`;
	}

	const days = Math.floor(now.diff(m, "days").as("days"));
	if (months >= 1) {
		return `${months.toFixed(0)}M ${(days % 30).toFixed(0)}d`;
	}

	const hours = Math.floor(now.diff(m, "hours").as("hours")) % 24;
	if (days >= 1) {
		return `${days.toFixed(2)}d ${hours.toFixed(2)}h`;
	}

	const mins = Math.floor(now.diff(m, "minutes").as("minutes")) % 60;
	if (hours >= 1) {
        return `${hours.toFixed(2)}h ${mins.toFixed(2)}m`;
    }

	const secs = Math.floor(now.diff(m, "seconds").as("seconds")) % 60;
	return `${mins.toFixed(2)}m ${secs.toFixed(2)}s`;
});
