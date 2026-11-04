<template>
    <span>
        <span v-if="value == null">
            --
        </span>

        <span v-else :style="{ color: textColor }" :title="'A skill of ' + value.toFixed(2) + ' is better than ' + percent.toFixed(2) + '% of other players (below 5 skill uncertainity)'">
            {{ percent.toFixed(0) }}%
        </span>
    </span>
</template>

<script lang="ts">
    import Vue from "vue";

    export const PercentileCell = Vue.extend({
        props: {
            value: { type: Number, required: true },
            percent: { type: Number, required: true }
        },

        computed: {
            textColor: function(): string {
                if (this.percent > 95) {
                    return "rgb(123, 0, 158)";
                }
                if (this.percent > 75) {
                    return "#038603";
                }

                return "#202020";
            }
        }
    });
    export default PercentileCell;
</script>