<template>
    <div>
        <collapsible header-text="Teams" size-class="h1" bg-color="bg-light">
            <div v-if="isMobile == false">
                <div v-if="isFunkyTeams == false" class="d-grid" :style="gridStyle">
                    <h4 v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID" class="ally-team-header mb-0" :style="getTeamNameStyle(allyTeam)">
                        Team {{ allyTeam.allyTeamID + 1 }}
                    </h4>

                    <template v-for="allyTeam in match.allyTeams">
                        <div
                            v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)"
                            :key="allyTeam.allyTeamID + '-' + player.teamID"
                            :style="getPlayerStyle(allyTeam, player, index)"
                            class="player-name"
                        >
                            <a :href="'/user/' + player.userID" style="text-decoration: none">
                                <span style="text-shadow: 1px 1px 1px #000000" :style="{ color: player.hexColor }">
                                    <img v-if="player.faction == 'Armada'" src="/img/armada.png" height="16" />
                                    <img v-else-if="player.faction == 'Cortex'" src="/img/cortex.png" height="16" />
                                    <img v-else-if="player.faction == 'Legion'" src="/img/legion.png" height="16" />
                                    <span v-else> ? </span>
                                    {{ player.username }}
                                </span>
                            </a>

                            <span v-if="player.handicap != 0" class="player-handicap">
                                <span v-if="player.handicap > 0" style="color: var(--bg-green)"> (+{{ player.handicap }}%) </span>
                                <span v-else> ({{ player.handicap }}%) </span>
                            </span>
                        </div>

                        <div
                            v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)"
                            :key="allyTeam.allyTeamID + '-os' + player.teamID"
                            :style="getPlayerOsStyle(allyTeam, player, index)"
                            class="player-os"
                        >
                            [<span class="font-monospace">{{ player.skill | locale(2) }}</span
                            >]
                        </div>
                    </template>
                </div>

                <div v-else>
                    <small class="text-muted"
                        >These are not usual team size! Gex is not programmed to handle teams of this size, so a fallback is being used instead</small
                    >

                    <table class="table table-sm" style="table-layout: fixed">
                        <thead>
                            <tr>
                                <th
                                    v-for="allyTeam in match.allyTeams"
                                    :key="allyTeam.allyTeamID"
                                    :style="{
                                        'background-color': allyTeamColor(allyTeam.allyTeamID),
                                    }"
                                >
                                    Team {{ allyTeam.allyTeamID + 1 }}
                                </th>
                            </tr>
                        </thead>

                        <tbody>
                            <tr v-for="index in maxTeamSize - 1" :key="index">
                                <td v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID">
                                    <span v-if="index > allyTeam.playerCount"></span>

                                    <player-cell v-else :player="playersByTeam(allyTeam.allyTeamID)[index - 1]"></player-cell>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </div>

            <div v-else>
                <div v-for="allyTeam in match.allyTeams" :key="allyTeam.allyTeamID" class="mb-2">
                    <h4 class="ally-team-header mb-0" :style="getTeamNameStyle(allyTeam)">Team {{ allyTeam.allyTeamID + 1 }}</h4>

                    <div
                        v-for="(player, index) in playersByTeam(allyTeam.allyTeamID)"
                        :key="allyTeam.allyTeamID + '-' + player.teamID"
                        :style="getPlayerStyle(allyTeam, player, index)"
                        class="player-name"
                    >
                        <a :href="'/user/' + player.userID" style="text-decoration: none">
                            <span style="text-shadow: 1px 1px 1px #000000" :style="{ color: player.hexColor }">
                                <img v-if="player.faction == 'Armada'" src="/img/armada.png" height="16" />
                                <img v-else-if="player.faction == 'Cortex'" src="/img/cortex.png" height="16" />
                                <img v-else-if="player.faction == 'Legion'" src="/img/legion.png" height="16" />
                                <span v-else> ? </span>
                                {{ player.username }}
                            </span>
                        </a>

                        [<span class="font-monospace" style="font-size: 0.9rem">{{ player.skill | locale(2) }}</span
                        >]

                        <span v-if="player.handicap != 0" class="player-handicap">
                            <span v-if="player.handicap > 0" style="color: var(--bg-green)"> (+{{ player.handicap }}%) </span>
                            <span v-else> ({{ player.handicap }}%) </span>
                        </span>
                    </div>
                </div>
            </div>

            <div v-if="match.spectators.length > 0">
                <h5>Spectators ({{ match.spectators.length }})</h5>

                <div class="d-flex flex-wrap">
                    <span v-for="spec in match.spectators" :key="spec.playerID" class="m-2">
                        <a :href="'/user/' + spec.userID">
                            {{ spec.username }}
                        </a>
                    </span>
                </div>
            </div>
        </collapsible>
    </div>
</template>

<style scoped>
    .ally-team-header {
        text-align: center;
        border-radius: 0.5rem;
        padding: 0.25rem 2rem;
        -webkit-text-stroke: #000 3px;
        paint-order: stroke fill;
    }

    .player-name {
        justify-self: stretch;
        text-align: end;
        margin-bottom: 0.5rem;
    }

    .player-handicap {
        font-size: 0.9rem;
    }

    .player-os {
        justify-self: stretch;
        text-align: start;
        margin-bottom: 0.5rem;
        font-size: 0.9rem;
    }
</style>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import Collapsible from "components/Collapsible.vue";

    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

    type GroupedPlayers = {
        allyTeamID: number;
        players: BarMatchPlayer[];
    };

    const PlayerCell = Vue.extend({
        props: {
            player: { type: Object as PropType<BarMatchPlayer>, required: true },
        },

        template: `
            <span>
                <a :href="'/user/' + player.userID" style="text-decoration: none; text-shadow: 1px 1px 1px black;">
                    <span :style="{ 'color': player.hexColor }">
                        [<span class="font-monospace">{{ player.skill | locale(2) }}</span>]
                        {{ player.username }}
                    </span>
                </a>    
                <span v-if="player.handicap != 0">
                    <span v-if="player.handicap > 0" style="color: var(--bg-green)">
                        (+{{ player.handicap }}%)
                    </span>
                    <span v-else>
                        ({{ player.handicap }}%)
                    </span>
                </span>
            </span>
        `,
    });

    export const MatchTeams = Vue.extend({
        props: {
            match: { type: Object as PropType<BarMatch>, required: true },
        },

        data: function () {
            return {};
        },

        methods: {
            allyTeamColor: function (allyTeamID: number): string {
                return this.match.players.find((iter) => iter.allyTeamID == allyTeamID)?.hexColor ?? `#333333`;
            },

            playersByTeam: function (allyTeamID: number): BarMatchPlayer[] {
                return [...this.match.players.filter((iter) => iter.allyTeamID == allyTeamID)].sort((a, b) => b.skill - a.skill);
            },

            getTeamNameStyle(allyTeam: BarMatchAllyTeam) {
                return {
                    "grid-column": `${((allyTeam.allyTeamID * 2) % 8) + 1} / span 2`,
                    "grid-row": this.getTeamNameRow(allyTeam),
                    "background-color": this.allyTeamColor(allyTeam.allyTeamID),
                };
            },

            getPlayerStyle(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number) {
                return {
                    "grid-column": ((player.allyTeamID * 2) % 8) + 1,
                    "grid-row": this.getPlayerRow(allyTeam, player, index),
                };
            },

            getPlayerOsStyle(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number) {
                return {
                    "grid-column": ((player.allyTeamID * 2) % 8) + 2,
                    "grid-row": this.getPlayerRow(allyTeam, player, index),
                };
            },

            getTeamNameRow(allyTeam: BarMatchAllyTeam): number {
                return Math.floor(allyTeam.allyTeamID / 4) * 4 + 2;
            },

            getPlayerRow(allyTeam: BarMatchAllyTeam, player: BarMatchPlayer, index: number): number {
                const v = Math.floor(allyTeam.allyTeamID / 4) * 4 + 1 + Math.floor(index % 8) + 2;
                //console.log(`MatchTeams> player ${player.username} on all ${allyTeam.allyTeamID} at index ${index} goes to ${v}`);
                return v;
            },
        },

        computed: {
            maxTeamSize: function (): number {
                return Math.max(...this.match.allyTeams.map((iter) => iter.playerCount));
            },

            isFunkyTeams: function (): boolean {
                return this.maxTeamSize > 8;
            },

            playersByAllyTeam: function (): GroupedPlayers[] {
                return this.match.allyTeams.map((iter) => {
                    return {
                        allyTeamID: iter.allyTeamID,
                        players: this.match.players.filter((p) => p.allyTeamID == iter.allyTeamID),
                    };
                });
            },

            gridStyle: function () {
                // show at most 8 teams per row
                const count: number = Math.min(8, this.match.allyTeams.length * 2);
                return {
                    "grid-template-columns": `repeat(${count}, 1fr)`,
                    "justify-items": "center",
                    "column-gap": "0.5rem",
                };
            },

            isMobile: function (): boolean {
                return window.screen.width < 800;
            },
        },

        components: {
            Collapsible,
            PlayerCell,
        },
    });
    export default MatchTeams;
</script>
