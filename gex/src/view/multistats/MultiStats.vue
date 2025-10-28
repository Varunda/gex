<template>
    <div class="">

        <div v-if="step == 'load_matches'">
            <h2>select matches</h2>

            <div class="mb-3">
                <label>match IDs, newline seperated</label>
                <textarea v-model="matchIdInput" placeholder="list of match IDs, newline seperated" class="form-control"></textarea>
                <button class="btn btn-primary" @click="loadMatchesByTextarea">load</button>
            </div>

            <div class="mb-3">
                <label>load by pool</label>
                <div class="input-group">
                    <select v-if="pools.state == 'loaded'" v-model.number="selectedPoolID" class="form-control">
                        <option v-for="pool in pools.data" :key="pool.id" :value="pool.id">
                            {{ pool.name }}
                        </option>
                    </select>

                    <button class="btn btn-primary" @click="loadMatchesOfPool(selectedPoolID)">
                        load
                    </button>
                </div>
            </div>
        </div>

        <div v-else-if="step == 'pick_ally_teams'">
            <h2>picking teams to show stats for</h2>

            <div v-if="matches.state == 'idle'"></div>

            <div v-else-if="matches.state == 'loading'">
                loading...

                <div v-if="loading.show" class="mb-2">
                    {{ loading.done }}/{{ loading.total }}
                    <div class="progress" style="height: 2rem;">
                        <div class="progress-bar" :style="{ 'width': `${loading.done / loading.total * 100}%`, 'height': '2rem' }"></div>
                    </div>
                </div>
            </div>

            <div v-else-if="matches.state == 'loaded'">

                <div class="mb-3 border-bottom pb-3">
                    <h6>select matches of player</h6>
                    <div>
                        <button class="btn btn-primary" @click="selectAllAllyTeams">
                            select all
                        </button>

                        <button v-for="player in allPlayers" :key="player.userID" class="btn btn-primary me-2" @click="selectMatchesOfPlayer(player.userID)">
                            {{ player.username }}
                        </button>
                    </div>
                </div>

                <table class="table">
                    <thead>
                        <tr class="table-secondary">
                            <th>match</th>
                            <th>map</th>
                            <th>start time</th>
                            <th>duration</th>
                            <th>no team</th>
                            <th v-for="i in maxAllyTeamCount" :key="i">
                                ally team {{ i }}
                            </th>
                        </tr>
                    </thead>

                    <tbody>
                        <tr v-for="match in matches.data" :key="match.id">
                            <td>
                                <a :href="'/match/' + match.id" class="font-monospace">{{ match.id }}</a>
                                <span v-if="match.processing == null || match.processing.actionsParsed == null" class="bi bi-cone text-warning"
                                    title="This game has not been fully processed!"></span>
                            </td>
                            <td>
                                {{ match.map }}
                            </td>
                            <td>
                                {{ match.startTime | moment }}
                            </td>
                            <td>
                                {{ match.durationMs / 1000 | mduration }}
                            </td>
                            <td>
                                <button class="btn" @click="match.pickedAllyTeams = []"
                                    :class="[ match.pickedAllyTeams.length == 0 ? 'btn-primary' : 'btn-secondary' ]">
                                    
                                    skip match
                                </button>
                            </td>
                            <td v-for="i in maxAllyTeamCount" :key="match.id + '-' + i">
                                <span v-if="i > match.allyTeams.length"></span>

                                <button v-else class="btn" @click="toggleAllyTeam(match, match.allyTeams.at(i - 1).allyTeamID)"
                                    :class="[ (match.pickedAllyTeams.indexOf(match.allyTeams.at(i - 1).allyTeamID) > -1) ? 'btn-primary' : 'btn-secondary' ]">

                                    ally team {{ match.allyTeams.at(i - 1).allyTeamID }}
                                    -
                                    <span v-for="player in match.players.filter(iter => iter.allyTeamID == match.allyTeams.at(i - 1).allyTeamID)">
                                        {{ player.username }}
                                    </span>
                                </button>
                            </td>
                        </tr>
                    </tbody>
                </table>
            </div>

            <button class="btn btn-primary" @click="loadMatchStats">
                load stats for {{ selectedMatches.length }} matches
            </button>

            <button class="btn btn-danger" @click="clearSelection">
                clear selection
            </button>
        </div>

        <div v-else-if="step == 'show'" class="container remove-container-padding">
            <div>
                <h2>showing stats!</h2>

                <input v-model="show.name" class="form-control" placeholder="Name to display at top. leave blank for none"/>

                <toggle-button v-model="show.tables">show tables</toggle-button>

                <button class="btn btn-primary" @click="saveMatchSetup">
                    save url
                </button>
            </div>

            <hr class="border"/>

            <div v-if="stats.load.state == 'idle'"></div>

            <div v-else-if="stats.load.state == 'loading'">
                <busy></busy>
                loading game events...

                <div v-if="loading.show" class="mb-2">
                    {{ loading.done }}/{{ loading.total }}
                    <div class="progress" style="height: 2rem;">
                        <div class="progress-bar" :style="{ 'width': `${loading.done / loading.total * 100}%`, 'height': '2rem' }"></div>
                    </div>
                </div>
            </div>

            <div v-else-if="stats.load.state == 'loaded'">
                <h1 v-if="show.name != ''" class="wt-header bg-light text-dark mb-2">
                    {{ show.name }}
                </h1>

                <h3 class="bg-secondary border-dark rounded p-3 mb-2">
                    Stats for {{ selectedPlayers.map(iter => iter.username).join(", ") }}
                    over {{ selectedMatches.length }} matches
                </h3>

                <h2 class="wt-header bg-light text-dark">Combat stats</h2>

                <div class="d-flex flex-wrap align-items-center" style="gap: 1rem; justify-content: space-evenly;">
                    <div v-for="mostUsed in playerMostUsed" class="text-center border position-sticky" :key="mostUsed.defID" style="border-radius: 0.5rem;">
                        <div class="text-outline px-2 py-1" style="position: absolute; top: 0; background-color: #00000066; border-radius: 0.25rem 0 0.25rem 0;">
                            {{ mostUsed.name }}
                        </div>

                        <img :src="'/image-proxy/UnitPic?defName=' + mostUsed.defName" height="128" width="128" :title="mostUsed.name" style="border-radius: 0.5rem 0.5rem 0 0;">
                        <div>
                            <div>
                                {{ mostUsed.produced | locale(0) }} made
                            </div>

                            <div>
                                {{ mostUsed.kills | locale(0) }} kills
                            </div>
                        </div>
                    </div>

                    <div class="text-center">
                        <h4>Metal efficiency - {{ totalMetalKilled / totalMetalLost * 100 | locale(0) }}%</h4>

                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-metal-efficiency" height="200"></canvas>
                        </div>
                    </div>

                    <div class="text-center">
                        <h4>Damage efficiency - {{ totalDamageDealt / totalDamageTaken * 100 | locale(0) }}%</h4>
                        <div style="height: 200px; max-height: 200px">
                            <canvas id="combat-damage" height="200"></canvas>
                        </div>
                    </div>

                    <div class="text-center">
                        <h2>{{ stats.unitStats.reduce((acc, iter) => acc += iter.mobileKills, 0) | compact }}</h2>
                        <h4>Units killed</h4>

                        <h2>{{ stats.unitStats.reduce((acc, iter) => acc += iter.staticKills, 0) | compact }}</h2>
                        <h4>Buildings<br>destroyed</h4>
                    </div>

                </div>

                <a-table v-if="show.tables" :entries="dynamicUnits" display-type="table" default-sort-field="rank" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                    <a-col sort-field="name">
                        <a-header>
                            <h5 class="mb-0 text-center" style="min-width: 12rem"><b>Units</b></h5>
                        </a-header>

                        <a-body v-slot="entry">
                            <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                            {{ entry.name }}
                            <info-hover :text="entry.definition.tooltip"></info-hover>
                        </a-body>
                    </a-col>

                    <a-col sort-field="rank">
                        <a-header>
                            <b>Produced</b>
                            <info-hover text="How many of this unit were produced"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.produced }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="kills">
                        <a-header>
                            <b>Kills</b>
                            <info-hover text="How many kills these units got"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.kills == 0 }">
                                {{ entry.kills }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                            <info-hover text="How many of this unit were lost"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.lost == 0 }">
                                {{ entry.lost }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalRatio">
                        <a-header>
                            <b>Metal efficiency</b>
                            <info-hover text="Total metal worth of units killed by this type of unit"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalKilled == 0 }">
                                {{ entry.metalRatio * 100 | locale(0) }}%
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageDealt">
                        <a-header>
                            <b>Dmg dealt</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.damageDealt == 0 }">
                                {{ entry.damageDealt | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageRatio">
                        <a-header>
                            <b>Dmg eff</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.damageRatio == 0 }">
                                {{ entry.damageRatio * 100 | locale(0) }}%
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalKilled">
                        <a-header>
                            <b>Eco killed</b>
                            <info-hover text="The total metal and energy cost of units killed by this type of unit"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalKilled == 0 && entry.energyKilled == 0 }">
                                {{ entry.metalKilled | compact }}&nbsp;M
                                /
                                {{ entry.energyKilled | compact }}&nbsp;E
                            </span>
                        </a-body>
                    </a-col>
                </a-table>

                <a-table v-if="show.tables" :entries="staticUnits" display-type="table" default-sort-field="rank" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                    <a-col sort-field="name">
                        <a-header>
                            <h5 class="mb-0 text-center" style="min-width: 12rem">
                                <b>Structures</b>
                            </h5>
                        </a-header>

                        <a-body v-slot="entry">
                            <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                            {{ entry.name }}
                            <info-hover :text="entry.definition.tooltip"></info-hover>
                        </a-body>
                    </a-col>

                    <a-col sort-field="rank">
                        <a-header>
                            <b>Produced</b>
                            <info-hover text="How many of this unit were produced"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.produced }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="kills">
                        <a-header>
                            <b>Kills</b>
                            <info-hover text="How many kills these units got"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.kills == 0 }">
                                {{ entry.kills }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                            <info-hover text="How many of this unit were lost"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.lost == 0 }">
                                {{ entry.lost }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalRatio">
                        <a-header>
                            <b>Metal efficiency</b>
                            <info-hover text="Total metal worth of units killed by this type of unit"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalKilled == 0 }">
                                {{ entry.metalRatio * 100 | locale(0) }}%
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageDealt">
                        <a-header>
                            <b>Dmg dealt</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.damageDealt == 0 }">
                                {{ entry.damageDealt | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="damageRatio">
                        <a-header>
                            <b>Dmg eff</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.damageRatio == 0 }">
                                {{ entry.damageRatio * 100 | locale(0) }}%
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalKilled">
                        <a-header>
                            <b>Eco killed</b>
                            <info-hover text="The total metal and energy cost of units killed by this type of unit"></info-hover>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalKilled == 0 && entry.energyKilled == 0 }">
                                {{ entry.metalKilled | compact }}&nbsp;M
                                /
                                {{ entry.energyKilled | compact }}&nbsp;E
                            </span>
                        </a-body>
                    </a-col>
                </a-table>

                <h2 class="wt-header bg-light text-dark">
                    Economy stats
                </h2>

                <div class="d-flex flex-wrap align-items-center" style="gap: 1rem; justify-content: space-evenly;">
                    <div class="text-center">
                        <h2>{{ totalEcoStats.metalProduced | compact}}</h2>
                        <h5>Metal made</h5>

                        <h2>{{ totalEcoStats.energyProduced | compact }}</h2>
                        <h5>Energy made</h5>
                    </div>

                    <div class="text-center">
                        <h2>{{ totalMetalReclaim | compact }}</h2>
                        <h5>Metal reclaimed</h5>

                        <h2>{{ totalEnergyReclaim | compact }}</h2>
                        <h5>Energy reclaimed</h5>
                    </div>

                    <div class="text-center">
                        <h4>Metal excess - {{ totalEcoStats.metalExcess / totalEcoStats.metalProduced * 100 | locale(2) }}%</h4>

                        <div style="height: 200px; max-height: 200px">
                            <canvas id="eco-metal-excess" height="200"></canvas>
                        </div>
                    </div>

                    <div v-for="mostUsed in ecoMostEnergy" class="text-center border position-sticky" :key="mostUsed.definitionID" style="border-radius: 0.5rem;">
                        <div class="text-outline px-2 py-1" style="position: absolute; top: 0; background-color: #00000066; border-radius: 0.5rem 0 0.5rem 0;">
                            {{ mostUsed.name }}
                        </div>

                        <img :src="'/image-proxy/UnitPic?defName=' + mostUsed.defName" height="128" width="128" :title="mostUsed.name" style="border-radius: 0.5rem 0.5rem 0 0;">
                        <div>
                            <div>
                                {{ mostUsed.energyMade | compact }} E made
                            </div>

                            <div>
                                {{ mostUsed.count | locale(0) }} constructed
                            </div>
                        </div>
                    </div>
                </div>

                <a-table v-if="show.tables" :entries="builders" :hide-paginate="true" default-sort-field="rank" default-sort-order="desc" :default-page-size="10">
                    <a-col>
                        <a-header>
                            <h4 class="mb-0">
                                <b>Builders</b>
                            </h4>
                        </a-header>

                        <a-body v-slot="entry">
                            <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                            {{ entry.name }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="rank">
                        <a-header>
                            <b>Produced</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.count }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.lost == 0 }">
                                {{ entry.lost }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalUsed">
                        <a-header>
                            <b>Metal used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalUsed == 0 }">
                                {{ entry.metalUsed | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalMade">
                        <a-header>
                            <b>Metal made</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.metalMade == 0 }">
                                {{ entry.metalMade | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>Energy used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.energyUsed == 0 }">
                                {{ entry.energyUsed | compact }}
                            </span>
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>Energy made</b>
                        </a-header>

                        <a-body v-slot="entry">
                            <span :class="{ 'text-muted': entry.energyMade == 0 }">
                                {{ entry.energyMade | compact }}
                            </span>
                        </a-body>
                    </a-col>
                </a-table>

                <div v-if="show.tables" class="d-flex flex-wrap" style="gap: 1rem;">
                    <div class="flex-grow-1" style="flex-basis: 48%">
                        <a-table :entries="metalProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                            <a-col sort-field="name">
                                <a-header>
                                    <h4 class="mb-0" style="min-width: 12rem;">
                                        <b>Metal</b>
                                    </h4>
                                </a-header>

                                <a-body v-slot="entry">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    {{ entry.name }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="count">
                                <a-header>
                                    <b>Created</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.count }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="lost">
                                <a-header>
                                    <b>Lost</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.lost }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="reclaimed">
                                <a-header>
                                    <b>Reclaimed</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.reclaimed | locale(0) }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="metalMade">
                                <a-header>
                                    <b>M made</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.metalMade | compact }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="energyUsed">
                                <a-header>
                                    <b>E used</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.energyUsed | compact }}
                                </a-body>
                            </a-col>
                        </a-table>
                    </div>

                    <div class="flex-grow-1" style="flex-basis: 48%">
                        <a-table :entries="energyProduction" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                            <a-col sort-field="name">
                                <a-header>
                                    <h4 class="mb-0" style="min-width: 12rem;">
                                        <b>Energy</b>
                                    </h4>
                                </a-header>

                                <a-body v-slot="entry">
                                    <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                                    {{ entry.name }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="count">
                                <a-header>
                                    <b>Created</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.count }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="lost">
                                <a-header>
                                    <b>Lost</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.lost }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="reclaimed">
                                <a-header>
                                    <b>Reclaimed</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.reclaimed | locale(0) }}
                                </a-body>
                            </a-col>

                            <a-col sort-field="energyMade">
                                <a-header>
                                    <b>Energy made</b>
                                </a-header>

                                <a-body v-slot="entry">
                                    {{ entry.energyMade | compact }}
                                </a-body>
                            </a-col>
                        </a-table>
                    </div>
                </div>

                <a-table v-if="show.tables" :entries="factoryStats" default-sort-field="produced" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                    <a-col sort-field="name">
                        <a-header>
                            <h4 class="mb-0" style="min-width: 12rem;">
                                <b>Factories</b>
                            </h4>
                        </a-header>

                        <a-body v-slot="entry">
                            <unit-icon :name="entry.definitionName" :color="entry.definition.color" :size="24"></unit-icon>
                            {{ entry.name }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="produced">
                        <a-header>
                            <b>Created</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.produced }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.lost }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="reclaimed">
                        <a-header>
                            <b>Reclaimed</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.reclaimed | locale(0) }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="gamesUsedCount">
                        <a-header>
                            <b>Games used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.gamesUsed.size | locale(0) }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="unitsMade">
                        <a-header>
                            <b>Units made</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.unitsMade | compact }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalUsed">
                        <a-header>
                            <b>M used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.metalUsed | compact }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="energyUsed">
                        <a-header>
                            <b>E used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.energyUsed | compact }}
                        </a-body>
                    </a-col>
                </a-table>

                <a-table v-if="show.tables" :entries="otherBuildings" default-sort-field="count" default-sort-order="desc" :hide-paginate="true" :default-page-size="10">
                    <a-col sort-field="name">
                        <a-header>
                            <h4 class="mb-0" style="min-width: 12rem;">
                                <b>Other buildings</b>
                            </h4>
                        </a-header>

                        <a-body v-slot="entry">
                            <unit-icon :name="entry.defName" :color="entry.definition.color" :size="24"></unit-icon>
                            {{ entry.name }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="count">
                        <a-header>
                            <b>Created</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.count }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="lost">
                        <a-header>
                            <b>Lost</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.lost }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="reclaimed">
                        <a-header>
                            <b>Reclaimed</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.reclaimed | locale(0) }}
                        </a-body>
                    </a-col>

                    <a-col>
                        <a-header>
                            <b>Games used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.gamesUsed.size | locale(0) }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="metalUsed">
                        <a-header>
                            <b>M used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.metalUsed | compact }}
                        </a-body>
                    </a-col>

                    <a-col sort-field="energyUsed">
                        <a-header>
                            <b>E used</b>
                        </a-header>

                        <a-body v-slot="entry">
                            {{ entry.energyUsed | compact }}
                        </a-body>
                    </a-col>
                </a-table>

                <h2 class="wt-header bg-light text-dark">
                    Fun stats
                    <h4 class="d-inline">
                        (of a single unit)
                    </h4>
                </h2>

                <div class="d-flex flex-wrap mb-4 justify-content-center" style="gap: 1rem;">
                    <div v-for="stat in stats.fun" :key="stat.statName" class="mb-1 text-start" style="height: 114px; width: 434px;">
                        <div class="img-overlay max-width"></div>

                        <div class="position-absolute max-width img-map-parent" style="z-index: 0; font-size: 0">
                            <div :style="mapBackground(stat.map)" class="img-map-side img-map-left"></div>
                            <div :style="mapBackground(stat.map)" class="img-map-center"></div>
                            <div :style="mapBackground(stat.map)" class="img-map-side img-map-right"></div>
                        </div>

                        <div style="z-index: 10; position: relative; top: 50%; transform: translateY(-50%); left: 20px;">
                            <img :src="'/image-proxy/UnitPic?defName=' + stat.defName" width="80" height="80" class="d-inline corner-img me-2"/>

                            <div class="d-inline-flex flex-column align-items-start text-outline" style="vertical-align: top; max-width: calc(100% - 80px - 20px - 1rem);">
                                <h4 class="mb-0 text-outline2">
                                    {{ stat.statHeader }}
                                </h4>

                                <div>
                                    {{ stat.player }}'s {{ stat.unitName }}
                                    {{ stat.valueDisplay }}
                                </div>

                                <div>
                                    on {{ mapNameWithoutVersion(stat.map) }}
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>

            <div>
                <h2 class="wt-header bg-white text-dark">Matches</h2>

                <match-list :matches="selectedMatches"></match-list>
            </div>

        </div>

        <div v-else>
            unchecked step: {{ step }}
        </div>
    </div>
</template>

<style scoped>
    /* from: https://stackoverflow.com/a/61913549 by Temani Afif */
    .corner-img {
        --s: 8px; /* size on corner */
        --t: 1px; /* thickness of border */
        --g: 0px; /* gap between border//image */
        
        padding: calc(var(--g) + var(--t));
        outline: var(--t) solid var(--bs-white); /* color here */
        outline-offset: calc(-1*var(--t));
        mask:
            conic-gradient(at var(--s) var(--s),#0000 75%,#000 0)
            0 0/calc(100% - var(--s)) calc(100% - var(--s)),
            conic-gradient(#000 0 0) content-box;
    }

    .max-width {
        max-width: calc(100vw - (var(--bs-gutter-x) * 0.5)) !important;
    }

    .img-map-parent {
        max-width: 100vw;
        white-space: nowrap;
        overflow: hidden;
    }

    .img-overlay {
        width: 434px;
        height: 114px;
        position: absolute;
        background: #000a;
        /*
        background: linear-gradient(to right, #0008, var(--bs-body-bg));
        */
        z-index: 1;
    }

    .img-overlay-player {
        width: 360px;
        height: 114px;
        position: absolute;
        background: #000a;
        background: linear-gradient(to right, var(--bs-body-bg), #0008);
        z-index: 1;
    }

    .img-overlay-armada {
        background-color: #487edb44;
    }

    .img-overlay-cortex {
        background-color: #b93d3244;
    }

    .img-map-side {
        display: inline-block;
        width: 124px;
        height: 114px;
        transform: scaleX(-1);
        background-repeat: no-repeat !important;
        background-size: 150% !important;
    }

    .img-map-left {
        background-position: left -36px !important;
    }

    .img-map-center {
        display: inline-block;
        width: 186px;
        height: 114px;
        background-position: center -36px !important;
        background-size: cover !important;
        background-repeat: no-repeat !important;
    }

    .img-map-right {
        background-position: right -36px !important;
    }
</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import Busy from "components/Busy.vue";
    import ATable, { ABody, AFilter, AFooter, AHeader, ACol, ARank, ATableType } from "components/ATable";
    import UnitIcon from "components/app/UnitIcon.vue";
    import InfoHover from "components/InfoHover.vue";
    import ToggleButton from "components/ToggleButton";
    import MatchList from "components/app/MatchList.vue";

    import Toaster from "Toaster";
    import Chart, { ChartDataset, Element } from "chart.js/auto/auto.esm";
    import ChartDataLabels from "chartjs-plugin-datalabels";
    Chart.register(ChartDataLabels);
    Chart.defaults.color = "white";

    import { MatchPoolApi } from "api/MatchPoolApi";
    import { BarMatchApi } from "api/BarMatchApi";
    import { GameOutputApi } from "api/GameOutputApi";

    import { MatchPool } from "model/MatchPool";
    import { MatchPoolEntry } from "model/MatchPoolEntry";
    import { BarMatch } from "model/BarMatch";
    import { GameOutput } from "model/GameOutput";
    import { GameEventUnitDef } from "model/GameEventUnitDef";
    import { BarMatchPlayer } from "model/BarMatchPlayer";

    import "filters/MomentFilter";
    import "filters/DurationFilter";
    import "filters/LocaleFilter";
    import "filters/CompactFilter";

    import { ResourceProductionData, ResourceProductionEntry } from "view/match/compute/ResourceProductionData";
    import { UnitStats } from "view/match/compute/UnitStatData";
    import MergedStats from "view/match/compute/MergedStats";
    import { UnitPositionFrame } from "view/match/compute/UnitPositionFrame";
    import { FactoryData, PlayerFactories } from "view/match/compute/FactoryData";

    import CompactUtils from "util/Compact";
    import { MapUtil } from "util/MapUtil";
import LocaleUtil from "util/Locale";
import TimeUtils from "util/Time";

    type BarMatchAndSide = BarMatch & {
        pickedAllyTeams: number[];
    };

    type ResourceProductionEntryGameCount = ResourceProductionEntry & {
        gamesUsed: Set<string>;
    };

    type FactoryStats = {
        definitionName: string;
        name: string;
        definition: GameEventUnitDef;
        unitsMade: number;
        produced: number;
        reclaimed: number;
        lost: number;
        energyUsed: number;
        metalUsed: number;
        gamesUsed: Set<string>;
        gamesUsedCount: number;
    };

    type TotalEcoProduced = {
        metalProduced: number;
        metalExcess: number;
        energyProduced: number;
        energyExcess: number;
    }

    class UnitInstance {
        public unitID: number = 0;
        public defName: string = "";
        public statName: string = "";
        public statHeader: string = "";

        public unitName: string = "";
        public value: number = 0;
        public valueDisplay: string = "";

        public gameID: string = "";
        public map: string = "";
        public player: string = "";
    }

    export const MultiStats = Vue.extend({
        props: {

        },

        data: function() {
            return {
                pools: Loadable.idle() as Loading<MatchPool[]>,

                step: "load_matches" as "load_matches" | "pick_ally_teams" | "show",

                matchIds: [] as string[],

                selectedPoolID: 0 as number,
                matchIdInput: "" as string,

                matches: Loadable.idle() as Loading<BarMatchAndSide[]>,
                outputs: Loadable.idle() as Loading<GameOutput[]>,

                loading: {
                    show: false as boolean,
                    done: 0 as number,
                    total: 0 as number
                },

                show: {
                    tables: false as boolean,
                    name: "" as string
                },

                stats: {
                    load: Loadable.idle() as Loading<number>,
                    production: [] as ResourceProductionEntryGameCount[],
                    unitStats: [] as UnitStats[],
                    merged: [] as MergedStats[],
                    factory: [] as FactoryStats[],
                    fun: [] as UnitInstance[],
                },

                chart: {
                    metalEff: null as Chart | null,
                    damage: null as Chart | null,
                    metalExcess: null as Chart | null
                }
            }
        },

        created: function(): void {
            document.title = `Gex / Multi-stats`;
        },

        mounted: function(): void {
            const searchParams: URLSearchParams = new URLSearchParams(location.search);
            const matches: string | null = searchParams.get("matches");

            if (matches != null) {
                this.loadFromUrl();
            } else {
                this.loadPools();
            }
        },

        methods: {
            loadFromUrl: async function(): Promise<void> {
                const searchParams: URLSearchParams = new URLSearchParams(location.search);
                const matches: string | null = searchParams.get("matches");
                if (matches == null) {
                    return;
                }

                console.log(`MultiStats> loading matches and ally teams from url: ${decodeURIComponent(matches)}`);
                const matchParts: string[] = decodeURIComponent(matches).split(";");

                for (const mp of matchParts) {
                    const parts: string[] = mp.split(",");
                    if (parts.length != 2) {
                        console.warn(`MultiStats> missing comma split from match part [mp=${mp}]`);
                        continue;
                    }

                    this.matchIds.push(parts[0]);
                }

                console.log(`MultiStats> loading matches from url [count=${this.matchIds.length}]`);
                await this.loadMatches();

                if (this.matches.state != "loaded") {
                    console.error(`MultiStats> cannot keep loading from URL, matches.state is not loaded`);
                    return;
                }

                for (const mp of matchParts) {
                    const parts: string[] = mp.split(",");
                    if (parts.length != 2) {
                        console.warn(`MultiStats> missing comma split from match part [mp=${mp}]`);
                        continue;
                    }

                    const matchId: string = parts[0];
                    const allyTeams: string[] = parts[1].split("-");

                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == matchId);
                    if (match == undefined) {
                        console.warn(`MultiStats> missing match to set ally team [matchID=${matchId}]`);
                        continue;
                    }

                    match.pickedAllyTeams = allyTeams.map(iter => Number.parseInt(iter));
                }

                await this.loadMatchStats();
            },

            loadPools: async function(): Promise<void> {
                this.pools = Loadable.loading();
                this.pools = await MatchPoolApi.getAll();
            },

            loadMatchesOfPool: async function(poolID: number): Promise<void> {
                const res: Loading<MatchPoolEntry[]> = await MatchPoolApi.getEntriesByID(poolID);
                if (res.state != "loaded") {
                    Toaster.add(`Failed to load pool ${poolID}`, `failed to load pool, see console`, "danger");
                    return;
                }

                this.matchIds = res.data.map(iter => iter.matchID);
                await this.loadMatches();
            },

            loadMatchesByTextarea: async function(): Promise<void> {
                this.matchIds = this.matchIdInput.split("\n").map(iter => iter.trim()).filter(iter => iter.length > 0);
                await this.loadMatches();
            },

            loadMatches: async function(): Promise<void> {
                this.step = "pick_ally_teams";

                this.loading.show = true;
                this.loading.total = this.matchIds.length;
                this.loading.done = 0;

                this.matches = Loadable.loading();
                const loaded: BarMatch[] = [];
                for (const matchId of this.matchIds) {
                    const res: Loading<BarMatch> = await BarMatchApi.getByID(matchId);
                    this.loading.done += 1;

                    if (res.state == "loaded") {
                        loaded.push(res.data);
                    } else {
                        console.error(`MultiStats> failed to load match [matchId=${matchId}] [state=${res.state}]`);
                    }
                }

                this.loading.show = false;

                this.matches = Loadable.loaded(loaded.map(iter => {
                    return {
                        ...iter,
                        pickedAllyTeams: []
                    }
                }));
            },

            clearSelection: function(): void {
                if (this.matches.state != "loaded") {
                    return;
                }

                for (const match of this.matches.data) {
                    match.pickedAllyTeams = [];
                }
            },

            selectAllAllyTeams: function(): void {
                if (this.matches.state != "loaded") {
                    return;
                }

                for (const match of this.matches.data) {
                    match.pickedAllyTeams = match.allyTeams.map(iter => iter.allyTeamID);
                }
            },

            selectMatchesOfPlayer: function(userID: number): void {
                if (this.matches.state != "loaded") {
                    return;
                }

                for (const match of this.matches.data) {
                    let p: BarMatchPlayer | undefined = match.players.find(iter => iter.userID == userID);
                    if (p == undefined) {
                        continue;
                    }

                    match.pickedAllyTeams.push(p.allyTeamID);
                }
            },

            toggleAllyTeam: function(match: BarMatchAndSide, allyTeamID: number): void {
                if (match.pickedAllyTeams.indexOf(allyTeamID) == -1) {
                    console.log(`MultiStats> adding ally team to match [id=${match.id}] [allyTeamID=${allyTeamID}]`);
                    match.pickedAllyTeams.push(allyTeamID);
                } else {
                    console.log(`MultiStats> removing ally team from match [id=${match.id}] [allyTeamID=${allyTeamID}]`);
                    match.pickedAllyTeams = match.pickedAllyTeams.filter(iter => iter != allyTeamID);
                }
            },

            loadMatchStats: async function(): Promise<void> {
                this.step = "show";

                this.stats.load = Loadable.loading();
                this.outputs = Loadable.loading();

                this.loading.show = true;
                this.loading.total = this.selectedMatches.length;
                this.loading.done = 0;

                const outputs: GameOutput[] = [];
                for (const match of this.selectedMatches) {
                    console.log(`MultiStats> loading game output [matchId=${match.id}]`);
                    const res: Loading<GameOutput> = await GameOutputApi.getEvents(match.id);
                    ++this.loading.done;
                    
                    if (res.state == "loaded") {
                        outputs.push(res.data);
                    } else {
                        console.error(`MultiStats> failed to get game output [matchId=${match.id}] [state=${res.state}]`);
                    }
                }

                this.outputs = Loadable.loaded(outputs);
                console.log(`MultiStats> outputs loaded, computing stats`);

                // compute stats
                await this.$nextTick();
                console.time("MultiStats> resource data");
                this.computeResourceData();
                console.timeEnd("MultiStats> resource data");

                console.time("MultiStats> unit data");
                this.computeUnitStats();
                console.timeEnd("MultiStats> unit data");

                console.time("MultiStats> merged data");
                this.computeMergedStats();
                console.timeEnd("MultiStats> merged data");

                console.time("MultiStats> factory data");
                this.computeFactoryData();
                console.timeEnd("MultiStats> factory data");

                console.time("MultiStats> fun stats");
                this.makeFunStats();
                console.timeEnd("MultiStats> fun stats");

                // done
                this.stats.load = Loadable.loaded(0);
                console.log(`MultiStats> stats computed, generating charts`);

                await this.$nextTick();
                this.makeCharts();
                console.log(`MultiStats> done!`);
            },

            computeResourceData: function(): void {
                if (this.matches.state != "loaded" || this.outputs.state != "loaded") {
                    return;
                }

                const map: Map<string, ResourceProductionEntryGameCount> = new Map();

                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedKeys: Set<string> = new Set(match.pickedAllyTeams.map(iter => `ally-team-${iter}`));

                    const pdata: ResourceProductionData[] = ResourceProductionData.compute(match, output);
                    for (const pd of pdata) {
                        if (interestedKeys.has(pd.id) == false) {
                            continue;
                        }

                        for (const unit of pd.units) {
                            if (map.has(unit.defName) == false) {
                                map.set(unit.defName, {
                                    ...unit,
                                    gamesUsed: new Set([ output.gameID ])
                                });
                            } else {
                                const rpe: ResourceProductionEntryGameCount = map.get(unit.defName)!;
                                rpe.count += unit.count;
                                rpe.energyMade += unit.energyMade;
                                rpe.energyUsed += unit.energyUsed;
                                rpe.lost += unit.lost;
                                rpe.metalMade += unit.metalMade;
                                rpe.metalUsed += unit.metalUsed;
                                rpe.reclaimed += unit.reclaimed;
                                rpe.gamesUsed.add(output.gameID);
                                map.set(unit.defName, rpe);

                                if (rpe.defName == "armmoho") {
                                    console.log(`MultiStats> DEBUG armmoho metalMade ${unit.metalMade} ${pd.allyTeamID} ${pd.id}`);
                                }
                            }
                        }
                    }
                }

                this.stats.production = Array.from(map.values());
            },

            computeUnitStats: function(): void {
                if (this.matches.state != "loaded" || this.outputs.state != "loaded") {
                    return;
                }

                const map: Map<string, UnitStats> = new Map();

                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedKeys: Set<string> = new Set(match.pickedAllyTeams.map(iter => `ally-team-${iter}`));
                    const stats: UnitStats[] = UnitStats.compute(output, match);
                    for (const stat of stats) {
                        if (interestedKeys.has(stat.id) == false) {
                            continue;
                        }

                        const us: UnitStats | undefined = map.get(stat.defName);
                        if (us == undefined) {
                            map.set(stat.defName, stat);
                        } else {
                            us.rank += stat.rank;
                            us.buildPowerKilled += stat.buildPowerKilled;
                            us.damageDealt += stat.damageDealt;
                            us.damageTaken += stat.damageTaken;
                            us.energyKilled += stat.energyKilled;
                            us.kills += stat.kills;
                            us.lost += stat.lost;
                            us.metalKilled += stat.metalKilled;
                            us.mobileKills += stat.mobileKills;
                            us.produced += stat.produced;
                            us.reclaimed += stat.reclaimed;
                            us.reclaims += stat.reclaims;
                            us.staticKills += stat.staticKills;

                            us.damageRatio = us.damageDealt / Math.max(1, us.damageTaken);
                            us.metalRatio = us.metalKilled / Math.max(1, (us.produced * (us.definition?.metalCost ?? 1)));
                            us.energyRatio = us.energyKilled / Math.max(1, (us.produced * (us.definition?.energyCost ?? 1)));

                            map.set(stat.defName, us);
                        }
                    }
                }

                this.stats.unitStats = Array.from(map.values());
            },

            computeMergedStats: function(): void {
                if (this.matches.state != "loaded" || this.outputs.state != "loaded") {
                    return;
                }

                const mergedStats: MergedStats[] = [];
                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedTeamIds: Set<number> = new Set([
                        ...match.players.filter(iter => match.pickedAllyTeams.indexOf(iter.allyTeamID) > -1).map(iter => iter.teamID)
                    ]);

                    const merged: MergedStats[] = MergedStats.compute(match, output);
                    for (const m of merged) {
                        if (interestedTeamIds.has(m.teamID) == false) {
                            continue;
                        }

                        mergedStats.push(m);
                    }
                }

                this.stats.merged = mergedStats;
            },

            computeFactoryData: function(): void {
                if (this.matches.state != "loaded" || this.outputs.state != "loaded") {
                    return;
                }

                const map: Map<string, FactoryStats> = new Map();

                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedTeamIds: Set<number> = new Set([
                        ...match.players.filter(iter => match.pickedAllyTeams.indexOf(iter.allyTeamID) > -1).map(iter => iter.teamID)
                    ]);

                    const pfs: PlayerFactories[] = PlayerFactories.compute(match, output);
                    for (const pf of pfs) {
                        if (interestedTeamIds.has(pf.teamID) == false) {
                            continue;
                        }

                        for (const fac of pf.factories) {
                            const def: GameEventUnitDef | undefined = output.unitDefinitions.get(fac.factoryDefinitionID);
                            if (def == undefined) {
                                console.warn(`MultiStats> missing unit definition for factory [definitionID=${fac.factoryDefinitionID}] [gameID=${output.gameID}]`);
                                continue;
                            }

                            const facStats: FactoryStats = map.get(fac.factoryDefinitionName) ?? {
                                definitionName: fac.factoryDefinitionName,
                                definition: def,
                                name: def.name,
                                unitsMade: 0,
                                energyUsed: 0,
                                metalUsed: 0,
                                produced: 0,
                                lost: 0,
                                reclaimed: 0,
                                gamesUsed: new Set(),
                                gamesUsedCount: 0
                            };

                            facStats.unitsMade += fac.totalMade;
                            facStats.gamesUsed.add(match.id);

                            map.set(facStats.definitionName, facStats);
                        }
                    }
                }

                const arr: FactoryStats[] = Array.from(map.values());
                for (const fs of arr) {
                    const prodStats = this.stats.production.find(iter => iter.defName == fs.definitionName);
                    if (prodStats == undefined) {
                        console.warn(`MultiStats> missing production stats for factory [defName=${fs.definitionName}]`);
                        continue;
                    }

                    fs.energyUsed = prodStats.energyUsed;
                    fs.metalUsed = prodStats.metalUsed;
                    fs.produced = prodStats.count;
                    fs.lost = prodStats.lost;
                    fs.reclaimed = prodStats.reclaimed;
                    fs.gamesUsedCount = fs.gamesUsed.size;
                }

                this.stats.factory = arr.filter(iter => iter.produced > 0);
            },

            makeCharts: function(): void {
                this.makeMetalEffChart();
                this.makeDamageChart();
                this.makeMetalExcess();
            },

            makeMetalEffChart: function(): void {
                if (this.chart.metalEff != null) {
                    this.chart.metalEff.destroy();
                    this.chart.metalEff = null;
                }

                const canvas = document.getElementById("combat-metal-efficiency") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #combat-metal-efficiency`;
                }

                this.chart.metalEff = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: [ "Killed", "Lost" ],
                        datasets: [{
                            data: [
                                this.totalMetalKilled,
                                this.totalMetalLost
                            ],
                            backgroundColor: [
                                "#419d49",
                                "#ba3e33"
                            ],
                            datalabels: {
                                align: "top",
                                anchor: "center"
                            }
                        }]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: true
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: CompactUtils.compact
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                ticks: {
                                    display: false,
                                }
                            }
                        }
                    }
                });
            },

            makeDamageChart: function(): void {
                if (this.chart.damage != null) {
                    this.chart.damage.destroy();
                    this.chart.damage = null;
                }

                const canvas = document.getElementById("combat-damage") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #combat-damage`;
                }

                this.chart.damage = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: [ "Dealt", "Taken" ],
                        datasets: [
                            {
                                label: "Damage ratio",
                                data: [
                                    this.totalDamageDealt,
                                    this.totalDamageTaken
                                ],
                                backgroundColor: [
                                    "#419d49",
                                    "#ba3e33"
                                ]
                            }
                        ]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: true
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: CompactUtils.compact
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                ticks: {
                                    display: false,
                                }
                            }
                        }
                    }

                });
            },

            makeMetalExcess: function(): void {
                if (this.chart.metalExcess != null) {
                    this.chart.metalExcess.destroy();
                    this.chart.metalExcess = null;
                }

                const canvas = document.getElementById("eco-metal-excess") as HTMLCanvasElement | null; 
                if (canvas == null) {
                    throw `missing #eco-metal-excess`;
                }

                this.chart.damage = new Chart(canvas.getContext("2d")!, {
                    type: "bar",
                    data: {
                        labels: [ "Produced", "Excessed" ],
                        datasets: [
                            {
                                label: "Metal excessed",
                                data: [
                                    this.totalEcoStats.metalProduced,
                                    this.totalEcoStats.metalExcess
                                ],
                                backgroundColor: [
                                    "#419d49",
                                    "#ba3e33"
                                ]
                            }
                        ]
                    },
                    options: {
                        plugins: {
                            legend: {
                                display: false,
                            },
                            tooltip: {
                                enabled: true
                            },
                            datalabels: {
                                display: true,
                                color: "white",
                                font: {
                                    family: "Atkinson Hyperlegible",
                                    size: 18,
                                },
                                formatter: CompactUtils.compact
                            }
                        },
                        responsive: true,
                        maintainAspectRatio: false,
                        scales: {
                            y: {
                                ticks: {
                                    display: false,
                                }
                            }
                        }
                    }
                });
            },

            makeFunStats: function(): void {
                if (this.matches.state != "loaded" || this.outputs.state != "loaded") {
                    return;
                }

                const killsMap: Map<string, UnitInstance> = new Map();
                const posMap: Map<string, UnitInstance> = new Map();
                const dmgTakenMap: Map<string, UnitInstance> = new Map();
                const dmgPerMinMap: Map<string, UnitInstance> = new Map();

                const createdAtMap: Map<string, number> = new Map();
                const killedMap: Map<string, number> = new Map();
                const damageMap: Map<string, number> = new Map();

                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedTeamIds: Set<number> = new Set([
                        ...match.players.filter(iter => match.pickedAllyTeams.indexOf(iter.allyTeamID) > -1).map(iter => iter.teamID)
                    ]);

                    const unitToDefMap: Map<string, number> = new Map();
                    for (const made of output.unitsCreated) {
                        if (interestedTeamIds.has(made.teamID) == false) {
                            continue;
                        }

                        const key: string = `${output.gameID}-${made.unitID}`;
                        unitToDefMap.set(key, made.definitionID);
                        createdAtMap.set(key, made.frame);
                    }

                    for (const killed of output.unitsKilled) {
                        const key: string = `${output.gameID}-${killed.unitID}`;
                        killedMap.set(key, killed.frame);
                    }

                    for (const unit of output.unitDamage) {
                        const key: string = `${output.gameID}-${unit.unitID}`;
                        damageMap.set(key, unit.damageDealt);
                    }

                    for (const killed of output.unitsKilled) {
                        if (killed.attackerTeam == null || killed.attackerDefinitionID == null || killed.attackerID == null || interestedTeamIds.has(killed.attackerTeam) == false) {
                            continue;
                        }

                        const key: string = `${output.gameID}-${killed.attackerID}`;
                        const def: GameEventUnitDef | undefined = output.unitDefinitions.get(killed.attackerDefinitionID);

                        const inst: UnitInstance = killsMap.get(key) ?? {
                            unitID: killed.attackerID,
                            defName: def?.definitionName ?? `<no def ${killed.attackerDefinitionID}>`,
                            statHeader: "Most kills",
                            unitName: def?.name ?? `<no name ${killed.attackerDefinitionID}>`,
                            statName: "killed",
                            value: 0,
                            valueDisplay: "0",
                            gameID: killed.gameID,
                            player: match.players.find(iter => iter.teamID == killed.attackerTeam)?.username ?? `<missing team ${killed.attackerTeam}>`,
                            map: match.map
                        };

                        ++inst.value;

                        killsMap.set(key, inst);
                    }

                    const unitPositions: UnitPositionFrame[] = UnitPositionFrame.compute(match, output);

                    const prevPos: Map<string, UnitPositionFrame> = new Map();
                    for (const pos of unitPositions) {
                        if (interestedTeamIds.has(pos.teamID) == false) {
                            continue;
                        }

                        const key: string = `${output.gameID}-${pos.unitID}`;

                        const prevFrame: UnitPositionFrame | undefined = prevPos.get(key);
                        if (prevFrame == undefined) {
                            prevPos.set(key, pos);
                            continue;
                        }

                        const defID: number | undefined = unitToDefMap.get(key);
                        if (defID == undefined) {
                            continue;
                        }

                        const def: GameEventUnitDef | undefined = output.unitDefinitions.get(defID);
                        if (def?.definitionName == "armcom" || def?.definitionName == "corcom") {
                            continue;
                        }

                        const inst: UnitInstance = posMap.get(key) ?? {
                            unitID: pos.unitID,
                            defName: def?.definitionName ?? `<no def ${defID}>`,
                            statName: "Furthest traveled",
                            statHeader: "Furthest traveled",
                            unitName: def?.name ?? `<no def ${defID}>`,
                            value: 0,
                            valueDisplay: "0",
                            gameID: match.id,
                            player: match.players.find(iter => iter.teamID == pos.teamID)?.username ?? `<missing team ${pos.teamID}>`,
                            map: match.map
                        };

                        const dist: number = Math.sqrt(Math.pow(prevFrame.x - pos.x, 2) + Math.pow(prevFrame.z - pos.z, 2));
                        inst.value += dist;

                        posMap.set(key, inst);
                    }

                    for (const unit of output.unitDamage) {
                        const key: string = `${output.gameID}-${unit.unitID}`;
                        const def: GameEventUnitDef | undefined = output.unitDefinitions.get(unit.definitionID);

                        const inst: UnitInstance = dmgTakenMap.get(key) ?? {
                            unitID: unit.unitID,
                            defName: def?.definitionName ?? `<no def ${unit.definitionID}>`,
                            statHeader: "Most damage taken",
                            unitName: def?.name ?? `<no name ${unit.definitionID}>`,
                            statName: "tanked",
                            value: 0,
                            valueDisplay: "0",
                            gameID: unit.gameID,
                            player: match.players.find(iter => iter.teamID == unit.teamID)?.username ?? `<missing team ${unit.teamID}>`,
                            map: match.map
                        };

                        inst.value = unit.damageTaken;

                        dmgTakenMap.set(key, inst);

                        const createdAtFrame: number | undefined = createdAtMap.get(key);
                        const killedAtFrame: number | undefined = killedMap.get(key);

                        if (createdAtFrame == undefined || killedAtFrame == undefined) {
                            continue;
                        }

                        const lifespan: number = (killedAtFrame - createdAtFrame) / 30;
                        const dmgDealtInst: UnitInstance = {
                            unitID: unit.unitID,
                            defName: def?.definitionName ?? `<no def ${unit.definitionID}>`,
                            statHeader: "Highest damage per minute",
                            unitName: def?.name ?? `<no name ${unit.definitionID}>`,
                            statName: "dealt",
                            value: unit.damageDealt / lifespan,
                            valueDisplay: `${CompactUtils.compact(unit.damageDealt)} dmg in ${TimeUtils.duration(lifespan)}`,
                            gameID: unit.gameID,
                            player: match.players.find(iter => iter.teamID == unit.teamID)?.username ?? `<missing team ${unit.teamID}>`,
                            map: match.map
                        };

                        // ignore corbw (shuriken), as the dmg dealt isn't correctly capped
                        if (lifespan > 0.5 && dmgDealtInst.defName != "corbw") {
                            dmgPerMinMap.set(key, dmgDealtInst);
                        }
                    }
                }

                const mostKills = Array.from(killsMap.values()).sort((a, b) => {
                    return b.value - a.value;
                }).at(0) ?? new UnitInstance();
                mostKills.valueDisplay = `killed ${mostKills.value} units`;
                this.stats.fun.push(mostKills);

                const mostDist = Array.from(posMap.values()).sort((a, b) => {
                    return b.value - a.value;
                }).at(0) ?? new UnitInstance();
                mostDist.valueDisplay = `traveled ${CompactUtils.compact(mostDist.value)} elmos`;
                this.stats.fun.push(mostDist);

                const mostDmgTaken = Array.from(dmgTakenMap.values()).sort((a, b) => {
                    return b.value - a.value;
                }).at(0) ?? new UnitInstance();
                mostDmgTaken.valueDisplay = `took ${CompactUtils.compact(mostDmgTaken.value)} damage`;
                this.stats.fun.push(mostDmgTaken);

                const mostDmgDealt = Array.from(dmgPerMinMap.values()).sort((a, b) => {
                    return b.value - a.value;
                }).at(0) ?? new UnitInstance();
                this.stats.fun.push(mostDmgDealt);
            },

            isBuilder: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.definition.buildPower > 0 && entry.definition.isFactory == false;
            },

            isMetalProduction: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.definition.speed == 0 && (entry.definition.energyConversionCapacity > 0 || entry.definition.metalMake > 0 || entry.definition.isMetalExtractor > 0);
            },

            isEnergyProduction: function(entry: ResourceProductionEntry): boolean {
                return !!entry.definition && entry.energyMade > 0 && entry.definition.speed == 0;
            },

            saveMatchSetup: function(): void {
                const parms: URLSearchParams = new URLSearchParams();
                parms.set("matches", encodeURIComponent(this.saveUrl));

                const url = new URL(location.href);
                history.pushState({ path: url.href }, "", `/multistats?${parms.toString()}`);
            },

            mapBackground: function(map: string): any {
                return {
                    "background": `url("/image-proxy/MapNameBackground?map=${map}&size=texture-thumb")`
                };
            },

            mapNameWithoutVersion: function(name: string): string {
                return MapUtil.getNameNameWithoutVersion(name);
            },
        },

        computed: {
            maxAllyTeamCount: function(): number {
                if (this.matches.state != "loaded") {
                    return 0;
                }

                return Math.max(...this.matches.data.map(iter => iter.allyTeams.length));
            },

            selectedMatches: function(): BarMatchAndSide[] {
                if (this.matches.state != "loaded") {
                    return [];
                }

                return this.matches.data.filter(iter => iter.pickedAllyTeams.length > 0);
            },

            selectedPlayers: function(): BarMatchPlayer[] {
                const map: Map<number, BarMatchPlayer> = new Map();
                for (const match of this.selectedMatches) {
                    for (const player of match.players) {
                        if (match.pickedAllyTeams.indexOf(player.allyTeamID) == -1) {
                            continue;
                        }

                        map.set(player.userID, player);
                    }
                }

                return Array.from(map.values());
            },

            allPlayers: function(): BarMatchPlayer[] {
                if (this.matches.state != "loaded") {
                    return [];
                }

                const map: Map<number, BarMatchPlayer> = new Map();
                for (const match of this.matches.data) {
                    for (const player of match.players) {
                        map.set(player.userID, player);
                    }
                }

                return Array.from(map.values()).sort((a: BarMatchPlayer, b: BarMatchPlayer) => {
                    return a.username.localeCompare(b.username);
                });
            },

            saveUrl: function(): string {
                return `${this.selectedMatches.map(iter => `${iter.id},${iter.pickedAllyTeams.join("-")}`).join(";")}`;
            },

            playerMostUsed: function(): UnitStats[] {
                return [...this.stats.unitStats].filter(iter => {
                    return iter.definition && iter.definition?.weaponCount > 0;
                }).sort((a, b) => {
                    return b.metalKilled - a.metalKilled;
                }).slice(0, 3);
            },

            dynamicUnits: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.stats.unitStats.filter(iter => {
                    return (iter.definition?.speed ?? 0) > 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            staticUnits: function(): Loading<UnitStats[]> {
                return Loadable.loaded(this.stats.unitStats.filter(iter => {
                    return (iter.definition?.speed ?? 1) == 0 && (iter.definition?.weaponCount ?? 0) > 0;
                }));
            },

            totalMetalKilled: function(): number {
                return this.stats.unitStats.reduce((acc, iter) => acc += iter.metalKilled, 0);
            },

            totalMetalLost: function(): number {
                return this.stats.unitStats.reduce((acc, iter) => acc += (iter.lost * (iter.definition?.metalCost ?? 1)), 0);
            },

            totalDamageDealt: function(): number {
                return this.stats.unitStats.reduce((acc, iter) => acc += iter.damageDealt, 0);
            },

            totalDamageTaken: function(): number {
                return this.stats.unitStats.reduce((acc, iter) => acc += iter.damageTaken, 0);
            },

            ecoMostEnergy: function(): ResourceProductionEntryGameCount[] {
                return [...this.stats.production].filter(iter => {
                    return iter.definition && iter.energyMade > 0;
                }).sort((a, b) => {
                    return b.energyMade - a.energyMade;
                }).slice(0, 3);
            },

            buildPowerUsedAverage: function(): number {
                const sum: number = this.stats.merged.reduce((acc, iter) => acc += (iter.buildPowerUsed / Math.max(1, iter.buildPowerAvailable)) * 100, 0);

                return sum / Math.max(1, this.stats.merged.length);
            },

            totalEcoStats: function(): TotalEcoProduced {
                const ret: TotalEcoProduced = {
                    metalProduced: 0,
                    metalExcess: 0,
                    energyProduced: 0,
                    energyExcess: 0
                };

                if (this.outputs.state != "loaded" || this.matches.state != "loaded") {
                    return ret;
                }

                for (const output of this.outputs.data) {
                    const match: BarMatchAndSide | undefined = this.matches.data.find(iter => iter.id == output.gameID);
                    if (match == undefined) {
                        console.error(`MultiStats> missing match from game output [output.gameID=${output.gameID}]`);
                        continue;
                    }
                    if (match.pickedAllyTeams.length == 0) {
                        console.error(`MultiStats> match does not have a pickedAllyTeam`);
                        continue;
                    }

                    const interestedTeamIds: Set<number> = new Set([
                        ...match.players.filter(iter => match.pickedAllyTeams.indexOf(iter.allyTeamID) > -1).map(iter => iter.teamID)
                    ]);

                    const lastFrame: number = Math.max(...output.teamStats.map(iter => iter.frame));

                    for (const ev of output.teamStats) {
                        if (ev.frame != lastFrame) {
                            continue;
                        }

                        if (interestedTeamIds.has(ev.teamID) == false) {
                            continue;
                        }

                        ret.metalProduced += ev.metalProduced;
                        ret.metalExcess += ev.metalExcess;
                        ret.energyProduced += ev.energyProduced;
                        ret.energyExcess += ev.energyExcess;
                    }
                }

                return ret;
            },

            totalMetalReclaim: function(): number {
                return this.stats.production
                    .filter(iter => iter.definition && iter.definition.isMetalExtractor == 0 && iter.definition.metalMake == 0
                        && iter.definition.isCommander == false && iter.metalMade > 0
                        && iter.definition.isFactory == false && iter.definition.buildPower > 0)
                    .reduce((acc, iter) => acc += iter.metalMade, 0);
            },

            totalEnergyReclaim: function(): number {
                return this.stats.production
                    .filter(iter => iter.definition && iter.definition.windGenerator == 0 && iter.definition.energyProduction == 0 && iter.definition.energyUpkeep >= 0
                        && iter.definition.isCommander == false && iter.energyMade > 0
                        && iter.definition.isFactory == false && iter.definition.buildPower > 0)
                    .reduce((acc, iter) => acc += iter.energyMade, 0);
            },

            builders: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.stats.production.filter(this.isBuilder));
            },

            metalProduction: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.stats.production.filter(this.isMetalProduction));
            },

            energyProduction: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.stats.production.filter(this.isEnergyProduction));
            },

            factoryProduction: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.stats.production.filter(iter => {
                    return iter.definition
                        && this.isMetalProduction(iter) == false
                        && this.isEnergyProduction(iter) == false
                        && this.isBuilder(iter) == false
                        && iter.definition.speed == 0
                        && iter.definition.isFactory == true;
                }));
            },

            factoryStats: function(): Loading<FactoryStats[]> {
                return Loadable.loaded(this.stats.factory);
            },

            otherBuildings: function(): Loading<ResourceProductionEntry[]> {
                return Loadable.loaded(this.stats.production.filter(iter => {
                    return iter.definition
                        && this.isMetalProduction(iter) == false
                        && this.isEnergyProduction(iter) == false
                        && this.isBuilder(iter) == false
                        && iter.definition.speed == 0
                        && iter.definition.isFactory == false
                        && (iter.metalUsed > 0 || iter.energyUsed > 0);
                }));
            },
        },

        components: {
            Busy, UnitIcon, InfoHover, ToggleButton,
            ATable, AHeader, ABody, AFooter, AFilter, ACol,
            MatchList
        }
    });
    export default MultiStats;

</script>