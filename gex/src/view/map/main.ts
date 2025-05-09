
import Vue from "vue";

import MapView from "./Map.vue";

const vm = new Vue({
	el: "#app",

	created: function(): void {

	},

	data: {

	},

	methods: {

	},

	components: {
        MapView
	}
});
(window as any).vm = vm;
