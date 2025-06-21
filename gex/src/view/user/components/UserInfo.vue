
<template>
    <div>
        <h2>
            {{ user.username }}
        </h2>

        <h4>
            <a :href="'https://www.bar-stats.pro/playerstats?playerName=' + user.username" target="_blank" ref="nofollow">BarStats link</a>
        </h4>

        <div class="mb-3">
            <h4 class="wt-header mb-3">
                Skill
            </h4>

            <div class="d-flex flex-wrap justify-content-around" style="gap: 1rem;">
                <div v-for="skill in skills" :key="skill.gamemode" class="hoverable text-center mx-2 rounded p-3">
                    <h5 class="border-bottom py-1">
                        {{ skill.gamemode | gamemode }}
                    </h5>

                    <div>
                        {{ skill.skill | locale(2) }}
                        <span class="text-muted">
                            &plusmn;{{ skill.skillUncertainty | locale(2) }}
                        </span>
                    </div>
                </div>
            </div>
        </div>

        <div>
            <h4 class="wt-header">
                Faction
            </h4>

            <div class="d-flex flex-wrap justify-content-around mb-3" style="gap: 0.5rem;">

                <div v-for="faction in user.factionStats" :key="faction.faction + '-' + faction.gamemode" class="hoverable text-center p-3 rounded">

                    <h5 class="border-bottom py-1">
                        <img v-if="faction.faction == 1" src="/img/armada.png" width="24">
                        <img v-else-if="faction.faction == 2" src="/img/cortex.png" width="24">
                        <img v-else-if="faction.faction == 3" src="/img/legion.png" width="24">
                        <span v-else-if="faction.faction == 4" src="/img/legion.png" style="width: 24px;">?</span>

                        {{ faction.faction | faction }}
                    </h5>

                    <div class="text-small fs-6">
                        (For {{ faction.gamemode | gamemode }})
                    </div>

                    <div>
                        {{ faction.playCount }} plays - {{ faction.playCount / Math.max(1, totalPlays) * 100 | locale(0) }}%
                    </div>

                    <div>
                        <span style="color: var(--bs-green)">
                            {{ faction.winCount }}
                        </span>
                        /
                        <span style="color: var(--bs-red)">
                            {{ faction.lossCount }}
                        </span>

                        <span>
                            - {{ faction.winCount / Math.max(1, faction.playCount) * 100 | locale(0) }}%
                        </span>
                    </div>
                </div>

            </div>

        </div>

        <div v-if="user.previousNames.length > 1" class="mb-3">
            <h4 class="wt-header mb-2">Previous names</h4>

            <table class="table table-sm table-hover">
                <thead class="table-secondary">
                    <tr>
                        <th>User name</th>
                        <th>
                            Timestamp
                            <info-hover text="When Gex first saw a game with this name"></info-hover>
                        </th>
                    </tr>
                </thead>

                <tbody>
                    <tr v-for="name in user.previousNames" :key="name.userName">
                        <td>{{ name.userName }}</td>
                        <td>{{ name.timestamp | moment }}</td>
                    </tr>
                </tbody>
            </table>
        </div>

        <div>
            <h4 class="wt-header mb-1">
                Maps
            </h4>
            <h6 class="text-muted mb-3">
                Map stats are seperated into gamemode, so it is possible to have 1 map listed multiple times, each for a different gamemode
            </h6>

            <a-table :entries="mapData" :show-filters="true" default-sort-field="playCount" default-sort-order="desc" :default-page-size="10" :overflow-wrap="true">
                <a-col>
                    <a-header>
                        <b>Map</b>
                    </a-header>

                    <a-filter field="map" type="string" method="input"
                        :conditions="[ 'contains', 'equals' ]">
                    </a-filter>

                    <a-body v-slot="entry">
                        {{ entry.map }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Gamemode</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.gamemode | gamemode }}
                    </a-body>
                </a-col>

                <a-col sort-field="playCount">
                    <a-header>
                        <b>Play count</b>
                    </a-header>

                    <a-body v-slot="entry">
                        {{ entry.playCount }}
                    </a-body>
                </a-col>

                <a-col>
                    <a-header>
                        <b>Win/Loss</b>
                    </a-header>

                    <a-body v-slot="entry">
                        <span style="color: var(--bs-green)">
                            {{ entry.winCount }}
                        </span>
                        /
                        <span style="color: var(--bs-red)">
                            {{ entry.lossCount }}
                        </span>
                        <span>
                            ({{ entry.winCount / Math.max(1, entry.playCount) * 100 | locale(0) }}%)
                        </span>
                    </a-body>
                </a-col>
            </a-table>


        </div>

    </div>
</template>

<style scoped>

    .hoverable {
        border: var(--bs-border-width) var(--bs-border-style) var(--bs-border-color);
        transition: border-color 0.1s ease-in, background-color 0.1s ease-in;
    }

    .hoverable:hover {
        background-color: var(--bs-secondary-bg);
        border-color: var(--bs-white);
    }

</style>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserSkill } from "model/BarUserSkill";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    export const UserInfo = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        computed:  {

            mapData: function(): Loading<BarUserMapStats[] >{
                return Loadable.loaded(this.user.mapStats);
            },

            skills: function(): BarUserSkill[] {
                return [...this.user.skill].sort((a, b) => a.gamemode - b.gamemode);
            },

            totalPlays: function(): number {
                return this.user.factionStats.reduce((acc, iter) => acc += iter.playCount, 0);
            },

            mapTotalPlays: function(): number {
                return this.user.mapStats.reduce((acc, iter) => acc += iter.playCount, 0);
            }
        },

        components: {
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover
        }
    });
    export default UserInfo;
</script>