import Vue from "vue";

import MapList from "./Maps.vue";

const vm = new Vue({
	el: "#app",

	created: function(): void {

	},

	data: {

	},

	methods: {

	},

	components: {
        MapList
	}
});
(window as any).vm = vm;
