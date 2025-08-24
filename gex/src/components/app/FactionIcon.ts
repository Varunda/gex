import Vue from "vue";

export const FactionIcon = Vue.extend({
    props: {
        faction: { type: Number, required: true },
        width: { type: Number, required: false, default: 24 }
    },

    template: `
        <span>
            <img v-if="faction == 1" src="/img/armada.png" :width="width" title="icon for armada">
            <img v-else-if="faction == 2" src="/img/cortex.png" :width="width" title="icon for cortex">
            <img v-else-if="faction == 3" src="/img/legion.png" :width="width" title="icon for legion">
            <img v-else-if="faction == 4" src="/img/random.png" :width="width" title="icon for random">
        </span>
    `
});
export default FactionIcon;