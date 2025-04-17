
import Vue from "vue";

import Users from "./Users.vue";

const vm = new Vue({
	el: "#app",

	created: function(): void {

	},

	data: {

	},

	methods: {

	},

	components: {
        Users
	}
});
(window as any).vm = vm;
