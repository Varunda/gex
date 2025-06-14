
<template>
    <div>
        <collapsible header-text="Chat" bg-color="bg-light" size-class="h1">

            <div v-if="messages.length == 0">
                No chat messages
            </div>

            <div v-else>
                <div v-if="show == false" class="alert alert-warning text-center" @click="show = true">
                    Chat is not censored or filtered in any way, and may contain inappropriate language. Click here to view
                </div>

                <div v-else style="max-height: 400px; overflow-y: scroll;">

                    <div v-if="ShowMobile == true">
                        <div v-for="msg in messages" :key="msg.id" class="mb-3">
                            <div>
                                [<span class="font-monospace">{{ msg.timestamp }}</span>]
                                <b :style="{ 'color': msg.playerColor }">{{ msg.from }}</b>
                                &rarr;
                                <b :style="{ 'color': msg.color }">{{ msg.to }}</b>
                            </div>

                            <div>
                                {{ msg.message }}
                            </div>
                        </div>
                    </div>

                    <div v-else class="d-grid" style="grid-template-columns: min-content auto 1fr; column-gap: 0.25rem;">
                        <template v-for="msg in messages">
                            <div style="grid-column: 1;" class="text-nowrap my-2">
                                [<span class="font-monospace">{{ msg.timestamp }}</span>]
                                <span :style="{ 'color': msg.color }">
                                    ({{ msg.to }})
                                </span>
                            </div>
                            <div style="grid-column: 2;" class="text-end border-end pe-2 py-2">
                                <b :style="{ 'color': msg.playerColor }">{{ msg.from }}</b>
                            </div>
                            <div style="grid-column: 3;" class="my-2 ms-2">{{ msg.message }}</div>
                        </template>
                    </div>
                </div>
            </div>
        </collapsible>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";

    import TimeUtils from "util/Time";

    import Collapsible from "components/Collapsible.vue";

    type FullMessage = {
        id: number;
        from: string;
        to: string;
        color: string;
        timestamp: string;
        message: string;
        playerColor: string; 
    }

    export const MatchChat = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
            ShowMobile: { type: Boolean, required: true }
        },

        data: function() {
            return {
                show: false as boolean
            }
        },

        methods: {

            getIdName: function(id: number, allyTeamID: number = -2): string {
                if (id == 255) {
                    return "HOST";
                } else if (id == 254) {
                    return "Everyone";
                } else if (id == 253) {
                    return "Spec";
                } else if (id == 252) {
                    if (this.ShowMobile == true) {
                        return `Team ${allyTeamID + 1}`;
                    } else {
                        return `Allies [Team ${allyTeamID + 1}]`;
                    }
                } else {
                    return this.match.players.find(iter => iter.teamID == id)?.username
                        ?? this.match.spectators.find(iter => iter.playerID == id)?.username
                        ?? `<missing ${id}>`;
                }
            },

            getIdColor: function(id: number, allyTeamID: number = -2): string {
                if (id == 255) {
                    return "var(--bs-purple)";
                } else if (id == 254) {
                    return "#ffffff";
                } else if (id == 253) {
                    return "#ffff00";
                } else if (id == 252) {
                    return this.match.players.find(at => at.allyTeamID == allyTeamID)?.hexColor ?? "#00ff00";
                } else {
                    return "#aaaaaa";
                }
            },

            getPlayerAllyTeamId: function(id: number): number {
                return this.match.players.find(p => p.playerID == id)?.allyTeamID ?? -1;
            },
            
            getPlayerColor: function(id: number): string {
                return this.match.players.find(p => p.playerID == id)?.hexColor ?? "";
            }
        },

        computed: {

            messages: function(): FullMessage[] {
                return this.match.chatMessages.map((iter, index) => {
                    const allyTeamID = this.getPlayerAllyTeamId(iter.fromId);

                    return {
                        id: index,
                        from: this.getIdName(iter.fromId),
                        to: this.getIdName(iter.toId, allyTeamID),
                        color: this.getIdColor(iter.toId, allyTeamID),
                        playerColor: this.getPlayerColor(iter.fromId),
                        timestamp: TimeUtils.duration(iter.gameTimestamp),
                        message: iter.message
                    }
                });
            }

        },

        components: {
            Collapsible
        }
    });
    export default MatchChat;
</script>