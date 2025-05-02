<template>
    <span :id="'info-hover-' + ID"
            class="d-inline-block" data-bs-toggle="popover" data-bs-trigger="hover"
            :data-bs-content="text"
            :data-bs-html="AllowHtml">

        <span v-if="icon != null" :class="'bi-' + icon">

        </span>

        <span v-else class="bi-question-lg"></span>
    </span>

</template>

<script lang="ts">
    import Vue from "vue";

    import * as bootstrap from "bootstrap";

    export const InfoHover = Vue.extend({
        props: {
            text: { type: String, required: true },
            AllowHtml: { type: Boolean, required: false, default: false },
            icon: { type: String, required: false }
        },

        data: function () {
            return {
                ID: 0 as number,
                popover: null as bootstrap.Popover | null
            };
        },

        created: function () {
            this.ID = Math.floor(Math.random() * 10000);
        },

        mounted: function () {
            this.$nextTick(() => {
                this.bindElem();
            });
        },

        methods: {

            bindElem: function(): void {
                const elem: HTMLElement | null = document.getElementById(`info-hover-${this.ID}`);
                if (elem == null) {
                    console.error(`InfoHover> failed to find #info-hover-${this.ID}`);
                    return;
                }

                if (this.popover != null) {
                    this.popover.dispose();
                    this.popover = null;
                }

                this.popover = new bootstrap.Popover(elem);
            }

        },

        watch: {
            "text": function(): void {
                this.$nextTick(() => {
                    this.bindElem();
                });
            }

        }
    });


    export default InfoHover;
</script>