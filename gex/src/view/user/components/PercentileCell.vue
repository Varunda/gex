<template>
    <span>
        <span v-if="value == null">
            --
        </span>

        <span v-else :style="{ color: textColor }" :title="'A skill of ' + value.toFixed(2) + ' is better than ' + percent.toFixed(2) + '% of other players'">
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
                    return "#FFD700";
                }
                if (this.percent > 75) {
                    return "#46C646";
                }

                return "#ADD8E6";
            }
        }
    });
    export default PercentileCell;
</script>