<template>
    <div>
        user {{ user.userID }}
    </div>
    
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import { BarMatch } from "model/BarMatch";
    import { BarUser } from "model/BarUser";

    export const UserAAAA = Vue.extend({
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

        },

        components: {

        }
    });
    export default UserAAAA;
</script>