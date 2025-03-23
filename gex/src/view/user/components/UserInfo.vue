
<template>
    <div>
        <h2 class="wt-header">User info</h2>

        {{ user.username }}

        <div>
            <h4 class="wt-header">
                Skill
            </h4>

            <div class="d-flex" style="gap: 1rem;">
                <div v-for="skill in user.skill" :key="skill.gamemode">
                    {{ skill.gamemode | gamemode }}
                    {{ skill.skill }} &plusmn;{{ skill.skillUncertainty }}
                </div>
            </div>
        </div>

        <div>
            <h4 class="wt-header">
                Maps
            </h4>

            <div class="d-flex" style="gap: 1rem;">
                <div v-for="map in user.mapStats" :key="map.map">
                    <div>
                        {{ map.map }} ({{ map.gamemode | gamemode }})
                    </div>
                    <div>
                        <span style="color: var(--bs-green)">
                            {{ map.winCount }}
                        </span>
                        /
                        <span style="color: var(--bs-red)">
                            {{ map.lossCount }}
                        </span>
                        /
                        <span class="text-muted">
                            {{ map.tieCount }}
                        </span>
                    </div>
                </div>
            </div>
        </div>

    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarUser } from "model/BarUser";

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

        },

        components: {

        }
    });
    export default UserInfo;
</script>