
<template>
    <div>
        <h2 class="wt-header">
            Chat
        </h2>

        <div style="max-height: 400px; overflow-y: scroll;">
            <div class="d-grid" style="grid-template-columns: min-content min-content auto min-content 1fr; column-gap: 0.25rem; row-gap: 0.5rem;">
                <template v-for="msg in messages">
                    <div style="grid-column: 1;" class="text-nowrap font-monospace">[{{ msg.timestamp }}]</div>
                    <div style="grid-column: 2;" class="text-end">&lt;{{ msg.from }}</div>
                    <div style="grid-column: 3;">&rarr;</div>
                    <div style="grid-column: 4;" class="text-start">{{ msg.to }}&gt;</div>
                    <div style="grid-column: 5;">{{ msg.message }}</div>
                </template>
            </div>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";

    import TimeUtils from "util/Time";

    type FullMessage = {
        id: number;
        from: string;
        to: string;
        timestamp: string;
        message: string;
    }

    export const MatchChat = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

            getIdName: function(id: number): string {
                if (id == 255) {
                    return "HOST";
                } else if (id == 254) {
                    return "Everyone";
                } else if (id == 253) {
                    return "Spec";
                } else if (id == 252) {
                    return "Allies";
                } else {
                    return this.match.players.find(iter => iter.teamID == id)?.username ?? `<missing ${id}>`;
                }
            }
        },

        computed: {

            messages: function(): FullMessage[] {
                return this.match.chatMessages.map((iter, index) => {
                    return {
                        id: index,
                        from: this.getIdName(iter.fromId),
                        to: this.getIdName(iter.toId),
                        timestamp: TimeUtils.duration(iter.gameTimestamp),
                        message: iter.message
                    }
                });
            }

        },

        components: {

        }
    });
    export default MatchChat;
</script>