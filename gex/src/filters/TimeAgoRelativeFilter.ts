import Vue from "vue";
import * as luxon from "luxon";

Vue.filter("timeAgoRelative", (date: Date | number): string => {
	if (typeof date == "number") {
		date = new Date(date);
    }

    const m = luxon.DateTime.fromJSDate(date);
    const now = luxon.DateTime.fromJSDate(new Date(Date.now()));

	const years = now.diff(m, "years").as("years");
	const months = now.diff(m, "months").as("months") % 12;

	if (years >= 1) {
		return `${years.toFixed(0)}Y ${months.toFixed(0)}M ago`;
	}

	const days = Math.floor(now.diff(m, "days").as("days"));
	if (months >= 1) {
		return `${Math.round(months).toFixed(0)}M ago`;
	}

	const hours = Math.floor(now.diff(m, "hours").as("hours")) % 24;
	if (days >= 1) {
		return `${Math.round(days).toFixed(0)}d ago`;
	}

    return `today`;
});
