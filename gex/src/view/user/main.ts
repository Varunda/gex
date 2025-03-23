
import Vue from "vue";

import User from "./User.vue";

const vm = new Vue({
	el: "#app",

	created: function(): void {

	},

	data: {

	},

	methods: {

	},

	components: {
        User
	}
});
(window as any).vm = vm;
