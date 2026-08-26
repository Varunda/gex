<template>
    <div>
        <h2 class="wt-header border-0">
            Gamemode stats
        </h2>

        <user-gamemode-stat-view v-for="gamemode in groupedFactionData"
            :key="gamemode.gamemode" :gamemode="gamemode" :user="user" :matches="matches">
        </user-gamemode-stat-view>
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

    import { GroupedFaction, GroupedFactionGamemode } from "./common";
    import UserGamemodeStatView from "./UserGamemodeStatView.vue";

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
                const wrongSkill: Map<number, number> = new Map();

                for (const match of this.matches) {
                    if (match.wrongSkillValues == true) {
                        wrongSkill.set(match.gamemode, (wrongSkill.get(match.gamemode) ?? 0) + 1);
                        continue;
                    }

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
                        count: c,
                        sum: sum,
                        averageSkill: (skill.get(iter[0]) ?? 0) / Math.max(1, c),
                        averageSkillDiff: (diff.get(iter[0]) ?? 0),
                        wrongSkillCount: wrongSkill.get(iter[0]) ?? 0
                    }
                }).sort((a, b) => {
                    return b.sum.playCount - a.sum.playCount;
                });
            },

        },

        components: {
            UserGamemodeStatView
        }
    });
    export default UserFactionStats;
</script>