
export type GroupedFaction = {
    faction: number;
    playCount: number;
    winCount: number;
}

export type GroupedFactionGamemode = {
    gamemode: number;
    armada: GroupedFaction | null;
    cortex: GroupedFaction | null;
    legion: GroupedFaction | null;
    random: GroupedFaction | null;
    sum: GroupedFaction;
    averageSkillDiff: number;
    averageSkill: number;
}
