<template>
    <div>
        <h2 class="wt-header border-0">
            Faction stats
        </h2>

        <div v-for="faction in groupedFactionData" :key="faction.gamemode" class="mb-3">
            <div class="wt-header mb-0" style="white-space: nowrap; text-wrap: wrap;">
                <h5 class="ms-2 d-inline-block mb-0">
                    <strong>
                        {{ faction.gamemode | gamemode }}
                    </strong>
                </h5>

                <wbr/>

                <h6 class="d-inline-block mb-0">
                    {{ faction.sum.winCount / faction.sum.playCount * 100 | locale(0) }}% win rate over {{ faction.sum.playCount }} games
                </h6>
            </div>

            <table class="table table-sm mb-1">
                <thead>
                    <tr class="table-active">
                        <th class="ps-2">Faction</th>
                        <th>Plays</th>
                        <th>Wins</th>
                        <th>Win %</th>
                    </tr>
                </thead>
                
                <tbody>
                    <tr v-if="faction.armada" is="FactionStatsRow" :data="faction.armada" :faction="1"></tr>
                    <tr v-if="faction.cortex" is="FactionStatsRow" :data="faction.cortex" :faction="2"></tr>
                    <tr v-if="faction.legion" is="FactionStatsRow" :data="faction.legion" :faction="3"></tr>
                    <tr v-if="faction.random" is="FactionStatsRow" :data="faction.random" :faction="4"></tr>
                    <tr class="table-active" is="FactionStatsRow" :data="faction.sum" :faction="0"></tr>
                </tbody>
            </table>

            <span class="text-muted">
                Average opponent skill is 
                <span v-if="faction.averageSkillDiff > 0">
                    {{ faction.averageSkillDiff | locale(2) }} <abbr title="OpenSkill (elo)">OS</abbr> below this user
                </span>
                <span v-else>
                    {{ -1 * faction.averageSkillDiff | locale(2) }} <abbr title="OpenSkill (elo)">OS</abbr> above this user
                </span>
            </span>
        </div>
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { BarUser } from "model/BarUser";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/BarGamemodeFilter";
    import "filters/LocaleFilter";
    import "filters/BarFactionFilter";
    import "filters/BarGamemodeFilter";

    import { FactionUtil } from "util/Faction";
    import { GamemodeUtil } from "util/Gamemode";
    import ColorUtils from "util/Color";
    import TimeUtils from "util/Time";

    import { GroupedFaction, GroupedFactionGamemode } from "./common";

    const FactionStatsRow = Vue.extend({
        props: {
            faction: { type: Number, required: true },
            data: { type: Object as PropType<GroupedFaction>, required: false }
        },

        template: `
            <tr>
                <td>
                    <span v-if="faction == 0" class="ps-2"><b>Total</b></span>
                    <img v-else-if="faction == 1" src="/img/armada.png" width="24" title="icon for armada">
                    <img v-else-if="faction == 2" src="/img/cortex.png" width="24" title="icon for cortex">
                    <img v-else-if="faction == 3" src="/img/legion.png" width="24" title="icon for legion">
                    <img v-else-if="faction == 4" src="/img/random.png" width="24" title="icon for random">
                    <span v-else>
                        unchecked faction {{ faction }}
                    </span>
                    <span v-if="faction != 0">
                        {{ faction | faction }}
                    </span>
                </td>
                <template v-if="data == null">
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                    <td class="text-muted">--</td>
                </template>
                <template v-else>
                    <td>{{ data.playCount | locale(0) }}</td>
                    <td>{{ data.winCount | locale(0) }}
                    <td>{{ data.winCount / data.playCount * 100 | locale(0) }}%</td>
                </template>
            </tr>
        `
    });

    export const UserFactionStats = Vue.extend({
        props: {
            user: { type: Object as PropType<BarUser>, required: true },
            matches: { type: Array as PropType<BarMatch[]>, required: true }
        },

        data: function() {
            return {

            }
        },

        methods: {

        },

        computed: {
            groupedFactionData: function(): GroupedFactionGamemode[] {
                const skill: Map<number, number> = new Map();
                const count: Map<number, number> = new Map();
                const diff: Map<number, number> = new Map();

                for (const match of this.matches) {
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

        components: {
            FactionStatsRow

        }
    });
    export default UserFactionStats;
</script>