<template>
    <div class="input-group">
        <input type="datetime-local" :value="str" @input="handleInput" class="form-control" />
        <button v-if="AllowNull" class="btn btn-secondary" title="clear date" @click="updateValue(null)">&times;</button>
    </div>
</template>

<script lang="ts">
    import Vue, { PropType } from "vue";

    import DateUtil from "util/Date";

    export const DateTimeInput = Vue.extend({
        props: {
            value: { type: Date as PropType<Date | null>, required: false },
            AllowNull: { type: Boolean, required: false, default: false },
        },

        data: function() {
            return {
                date: new Date() as Date | null,
                str: "" as string
            }
        },

        created: function(): void {
            this.updateDate();
        },

        methods: {
            updateDate: function(): void {
                this.date = this.value;
                if (this.date != null) {
                    this.str = DateUtil.getLocalDateString(this.date);
                } else {
                    this.str = "";
                }
            },

            handleInput: function(ev: any): void {
                const target: HTMLInputElement = ev.target;
                this.updateValue(target.value);
            },

            updateValue: function(v: string | null): void {
                this.str = v ?? "";
                if (this.str == "") {
                    this.date = null;
                } else {
                    this.date = new Date(this.str);
                }

                this.$emit("input", this.date);
            }
        },

        watch: {
            value: function(): void {
                this.$nextTick(() => {
                    this.updateDate();
                });
            }
        },

        components: {

        }
    });
    export default DateTimeInput;

</script>