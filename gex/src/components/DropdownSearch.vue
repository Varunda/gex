<template>
    <!--
    <div id="input-group">
        <input class="form-control pr-0" placeholder="search..." v-model="search" :id="'search-input-' + ID" @keyup.enter="enterPress" />

        <span class="input-group-append">
            <button class="btn btn-primary" type="button" @click="performSearch">
                &#128270;
            </button>
        </span>
    </div>
    -->

    <div>
        <input v-model="search" class="form-control" :list="'datalist-' + ID" placeholder="search...">

        <datalist :id="'datalist-' + ID">
            <option v-for="v in values" :key="v.value" :value="v.value">
                {{ v.value }}
            </option>
        </datalist>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { SearchResult } from "model/SearchResult";

    export const DropdownSearch = Vue.extend({
        props: {
            value: { type: String, required: false },
            api: { type: Function, required: true }
        },

        data: function() {
            return {
                ID: Math.floor(Math.random() * 100000) as number,
                enterLockout: 0 as number,

                values: [] as SearchResult[],

                search: "" as string,
                searchInput: {} as HTMLElement,
            }
        },

        mounted: function(): void {
            this.search = this.value;

            this.api().then((value: Loading<SearchResult[]>) => {
                if (value.state != "loaded") {
                    return;
                }

                this.values = value.data;
            });
        },

        methods: {
            enterPress: function(): void {
                const lockoutDiff: number = this.enterLockout - Date.now();
                console.log(`DropdownSearch #${this.ID}> lockoutDiff is ${lockoutDiff}`);
                if (lockoutDiff >= 0) {
                    console.log(`enter lockout hit (diff is ${lockoutDiff})`);
                    this.enterLockout = Date.now(); // if the lockout is hit once, let the next enter key do the search
                    return;
                }

                const container: HTMLElement | null = document.querySelector(".tribute-container");
                if (container == null) {
                    console.warn(`DropdownSearch #${this.ID}> query .tribute-container nothing returned`);
                    this.performSearch();
                    return;
                }

                if (container.style.display != "none") {
                    console.log(`DropdownSearch #${this.ID}> not sending @do-search, auto-complete tab is opened`);
                    return;
                }

                this.performSearch();
                return;
            },

            performSearch: function(): void {
                console.log(`DropdownSearch #${this.ID}> emiting do search: '${this.search.trim()}'`);
                this.$emit("do-search", this.search.trim());
            },

            focus: function(): void {
                this.$nextTick(() => {
                    this.searchInput.focus();
                });
            },
        },

        watch: {
            search: function(): void {
                this.$emit("input", this.search);
            }
        }

    });

    export default DropdownSearch;
</script>