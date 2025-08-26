<template>
    <div>
        <h1 class="text-center border-bottom pb-2 mb-4">
            {{ user.username }}
        </h1>

        <div class="d-flex mb-3 flex-wrap" style="gap: 1rem;">
            <div class="flex-grow-1 text-center">
                <h2 class="border-bottom d-inline-block px-3">
                    Favorite faction
                </h2>

                <h4 class="mb-1">
                    <faction-icon :faction="mostPlayedFaction.faction" :width="48"></faction-icon>
                    {{ mostPlayedFaction.faction | faction }}
                </h4>

                <h5>
                    {{ mostPlayedFaction.winCount / mostPlayedFaction.playCount * 100 | locale(0) }}% won of 
                    {{ mostPlayedFaction.playCount | locale(0) }} played
                </h5>
            </div>

            <div class="flex-grow-1 text-center">
                <h2 class="border-bottom d-inline-block px-3">
                    Favorite gamemode
                </h2>

                <h4 class="mb-1">
                    {{ mostPlayedGamemode.gamemode | gamemode }}
                </h4>

                <h5>
                    {{ mostPlayedGamemode.sum.winCount / mostPlayedGamemode.sum.playCount * 100 | locale(0) }}% won of 
                    {{ mostPlayedGamemode.sum.playCount | locale(0) }} played
                </h5>
            </div>

            <div class="flex-grow-1 text-center">
                <h2 class="border-bottom d-inline-block px-3">
                    Favorite maps
                </h2>

                <div class="d-lg-none">
                    <div v-for="map in mostPlayedMaps" :key="map.map" class="mb-2">
                        <b>{{ map.map }}</b>
                        <div>
                            {{ map.winCount / map.playCount * 100 | locale(0) }}% won of 
                            {{ map.playCount }} played
                        </div>
                    </div>
                </div>

                <div class="d-none d-lg-grid" style="grid-template-columns: 1fr 1fr;">
                    <template v-for="map in mostPlayedMaps">
                        <div class="text-end">
                            <b>{{ map.map }}</b>
                        </div>
                        <div class="ps-1 text-start">
                            -
                            {{ map.winCount / map.playCount * 100 | locale(0) }}% won of 
                            {{ map.playCount }} played
                        </div>
                    </template>
                </div>
            </div>
        </div>

        <h6 class="ps-3">
            <a :href="'https://www.bar-stats.pro/playerstats?playerName=' + user.username" target="_blank" ref="nofollow">BarStats link</a>
        </h6>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";
    import { Loading, Loadable } from "Loading";

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    import "chartjs-adapter-luxon";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import StartSpotMap from "components/app/StartSpotMap.vue";
    import Busy from "components/Busy.vue";
    import { FactionIcon } from "components/app/FactionIcon";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserSkill } from "model/BarUserSkill";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";
    import { BarMap } from "model/BarMap";
    import { UserPreviousName } from "model/UserPreviousName";
    import { BarUserFactionStats } from "model/BarUserFactionStats";
    import { BarUserSkillChanges } from "model/BarUserSkillChanges";

    import { MapStatsApi } from "api/map_stats/MapStatsApi";
    import { MapApi } from "api/MapApi";
    import { BarUserApi } from "api/BarUserApi";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";
    import { GamemodeUtil } from "util/Gamemode";
    import ColorUtils from "util/Color";

    import { GroupedFactionGamemode } from "./common";

    export const UserHeader = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            GroupedFactionData: { type: Array as PropType<GroupedFactionGamemode[]>, required: true }
        },

        data: function() {
            return {
                faction: {
                    chart: null as Chart | null,
                }

            }
        },

        computed: {
            mostPlayedFaction: function(): BarUserFactionStats {
                // vetur moment doesn't know es2024 stuff
                const map: Map<number, BarUserFactionStats[]> = Map.groupBy(this.user.factionStats, (elem: BarUserFactionStats) => {
                    return elem.faction;
                });

                // TODO 2025-08-24: this kinda fuckin sucks yo, is there a better way for this?
                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).slice(0, 1).map(iter => {
                    const fac: BarUserFactionStats = {
                        faction: iter[0],
                        gamemode: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        lossCount: 0,
                        tieCount: 0,
                        lastUpdated: new Date(),
                        userID: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    };
                    return fac;
                })[0];
            },

            mostPlayedGamemode: function(): GroupedFactionGamemode {
                return [...this.GroupedFactionData].sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                })[0];
            },

            mostPlayedMaps: function(): BarUserMapStats[] {
                const map: Map<string, BarUserMapStats[]> = Map.groupBy(this.user.mapStats, (iter: BarUserMapStats) => iter.map);

                return Array.from(map.entries()).sort((a, b) => {
                    const aCount: number = a[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    const bCount: number = b[1].reduce((acc, iter) => acc += iter.playCount, 0);
                    return bCount - aCount;
                }).slice(0, 3).map(iter => {
                    const map: BarUserMapStats = {
                        map: iter[0],
                        gamemode: 0,
                        lastUpdated: new Date(),
                        lossCount: 0,
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        tieCount: 0,
                        userID: 0
                    };
                    return map;
                });
            }
        }
    });
    export default UserHeader;
</script>