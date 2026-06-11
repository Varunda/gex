
<template>
    <div>
        <div class="mb-4">
            <user-header :user="user" :grouped-faction-data="groupedFactionData"></user-header>
        </div>

        <div>
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

    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    Chart.defaults.font.family = "Atkinson Hyperlegible";
    import "chartjs-adapter-luxon";

    import ATable, { ABody, AFilter, AFooter, AHeader, ACol } from "components/ATable";
    import InfoHover from "components/InfoHover.vue";
    import StartSpotMap from "components/app/StartSpotMap.vue";
    import Busy from "components/Busy.vue";
    import { FactionIcon } from "components/app/FactionIcon";

    import UserHeader from "./UserHeader.vue";
    import UserCharts from "./UserCharts.vue";

    import { BarUser } from "model/BarUser";
    import { BarUserMapStats } from "model/BarUserMapStats";
    import { BarUserSkill } from "model/BarUserSkill";
    import { MapStatsStartSpot } from "model/map_stats/MapStatsStartSpot";
    import { BarMap } from "model/BarMap";
    import { UserPreviousName } from "model/UserPreviousName";
    import { BarUserFactionStats } from "model/BarUserFactionStats";
    import { BarUserSkillChanges } from "model/BarUserSkillChanges";
    import { BarMatch } from "model/BarMatch";
    import { BarMatchPlayer } from "model/BarMatchPlayer";
    import { BarMatchAllyTeam } from "model/BarMatchAllyTeam";

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
    import TimeUtils from "util/Time";
    import { GroupedFaction, GroupedFactionGamemode } from "./common";


    export const UserInfo = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Object as PropType<Loading<BarMatch[]>>, required: true }
        },

        data: function() {
            return {
                averageSkillDiffs: new Map() as Map<number, number>,
            }
        },

        mounted: function(): void {

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
            },


            groupedFactionData: function(): GroupedFactionGamemode[] {
                const skill: Map<number, number> = new Map();
                const count: Map<number, number> = new Map();
                const diff: Map<number, number> = new Map();

                if (this.matches.state == "loaded") {
                    for (const match of this.matches.data) {
                        if (skill.has(match.gamemode) == false) {
                            skill.set(match.gamemode, 0);
                            count.set(match.gamemode, 0);
                        }

                        let s: number = skill.get(match.gamemode) ?? 0;
                        let c: number = count.get(match.gamemode) ?? 0;
                        let d: number = diff.get(match.gamemode) ?? 0;

                        const player: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                        if (player == undefined) {
                            console.warn(`UserInfo> missing BarMatchPlayer from match where a user was a part of [gameID=${match.id}]`);
                            continue;
                        }

                        const enemyPlayers: BarMatchPlayer[] = match.players.filter(iter => iter.allyTeamID != player.allyTeamID);
                        if (enemyPlayers.length == 0) {
                            console.warn(`UserInfo> game is missing any opponents [gameID=${match.id}]`);
                            continue;
                        }

                        const totalSkill: number = enemyPlayers.reduce((acc, iter) => acc += iter.skill, 0);
                        const avgSkill: number = totalSkill / enemyPlayers.length;

                        const playerSkill: number = player?.skill ?? 0;
                        const skillDiff: number = playerSkill - avgSkill;
                        //console.log(`UserInfo> match ${match.id} player skill ${playerSkill} diff ${skillDiff}`);

                        if (Number.isNaN(skillDiff)) {
                            console.warn(`UserInfo> got NaN skill diff [gameID=${match.id}]`);
                            continue;
                        }

                        s += playerSkill;
                        c += 1;
                        d += skillDiff;

                        skill.set(match.gamemode, s);
                        count.set(match.gamemode, c);
                        diff.set(match.gamemode, d);
                    }

                    for (const iter of diff) {
                        const gamemode: number = iter[0];
                        const s: number = iter[1];
                        const c: number = count.get(gamemode) ?? 1;

                        diff.set(gamemode, s / Math.max(1, c));
                    }
                }

                const map: Map<number, GroupedFaction[]> = new Map();

                for (const faction of this.user.factionStats) {
                    if (faction.gamemode == 0) {
                        continue;
                    }

                    const factionData: GroupedFaction[] = (map.get(faction.gamemode) ?? []);
                    factionData.push({
                        faction: faction.faction,
                        playCount: faction.playCount,
                        winCount: faction.winCount,
                    });

                    map.set(faction.gamemode, factionData);
                }

                return Array.from(map.entries()).map(iter => {
                    const sum: GroupedFaction = {
                        faction: 0,
                        playCount: iter[1].reduce((acc, iter) => acc += iter.playCount, 0),
                        winCount: iter[1].reduce((acc, iter) => acc += iter.winCount, 0),
                    }

                    const c: number = count.get(iter[0]) ?? 1;

                    return {
                        gamemode: iter[0],
                        armada: iter[1].find(iter => iter.faction == FactionUtil.ARMADA) ?? null,
                        cortex: iter[1].find(iter => iter.faction == FactionUtil.CORTEX) ?? null,
                        legion: iter[1].find(iter => iter.faction == FactionUtil.LEGION) ?? null,
                        random: iter[1].find(iter => iter.faction == FactionUtil.RANDOM) ?? null,
                        sum: sum,
                        averageSkill: (skill.get(iter[0]) ?? 0) / Math.max(1, c),
                        averageSkillDiff: (diff.get(iter[0]) ?? 0)
                    }
                }).sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                });
            },

        },

        watch: {

            matches: function(): void {
                if (this.matches.state != "loaded") {
                    return;
                }

                this.makeWinRateDurationChart();

                const skill: Map<number, number> = new Map();
                const count: Map<number, number> = new Map();

                for (const match of this.matches.data) {
                    if (skill.has(match.gamemode) == false) {
                        skill.set(match.gamemode, 0);
                        count.set(match.gamemode, 0);
                    }

                    let s: number = skill.get(match.gamemode) ?? 0;
                    let c: number = count.get(match.gamemode) ?? 0;

                    const totalSkill: number = match.players.reduce((acc, iter) => acc += iter.skill, 0);
                    const avgSkill: number = totalSkill / match.players.length;

                    const player: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == this.user.userID);
                    const playerSkill: number = player?.skill ?? 0;

                    const diff: number = playerSkill - avgSkill;

                    s += diff;
                    c += 1;

                    skill.set(match.gamemode, s);
                    count.set(match.gamemode, c);
                }

                for (const iter of skill) {
                    const gamemode: number = iter[0];
                    const s: number = iter[1];

                    const c: number = count.get(gamemode) ?? 1;

                    this.averageSkillDiffs.set(gamemode, s / Math.max(1, c));
                }

                this.$forceUpdate();
            },

            "winrateDuration.gamemode": function(): void {
                this.makeWinRateDurationChart();
            }

        },

        components: {
            UserHeader,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            InfoHover, Busy,
            StartSpotMap, 
            FactionIcon,
            UserCharts,
        }
    });
    export default UserInfo;
</script>