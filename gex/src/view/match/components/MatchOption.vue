<template>
    <collapsible :header-text="name" size-class="h6" :show="false" class="flex-grow-1" bg-color="bg-dark">
        <div style="max-height: 400px; overflow-y: auto">
            <table class="table table-sm">
                <tbody>
                    <tr v-for="iter in Object.keys(options)" :key="iter">
                        <td>
                            {{ iter }}
                        </td>
                        <td>
                            <span v-if="options[iter] == ''" class="text-muted"> -- </span>
                            <span v-else style="word-break: break-all">
                                {{ options[iter] }}
                            </span>
                        </td>
                    </tr>
                </tbody>
            </table>
        </div>
    </collapsible>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Collapsible from "components/Collapsible.vue";

    export const MatchOption = Vue.extend({
        props: {
            name: { type: String, required: true },
            options: { type: Object, required: true },
        },

        data: function () {
            return {};
        },

        methods: {},

        components: {
            Collapsible,
        },
    });
    export default MatchOption;
</script>
