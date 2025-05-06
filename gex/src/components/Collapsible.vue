<template>
    <div>
        <div class="wt-header d-flex" :class="classes" data-bs-toggle="collapse" :data-bs-target="'#' + elementID">
            <span :id="'icon-' + elementID" class="fas fa-caret-down"></span>
            {{ HeaderText }}

            <slot name="header"></slot>
        </div>

        <div :id="elementID" class="collapse" :class="{ show: opened }">
            <slot></slot>
        </div>
    </div>
</template>

<script lang="ts">
    import Vue from "vue";

    export const Collapsible = Vue.extend({
        props: {
            HeaderText: { type: String, required: true },
            show: { type: Boolean, required: false, default: true },
            SizeClass: { type: String, required: false, default: "h2" },
            BgColor: { type: String, required: false, default: "bg-secondary" },
        },

        data: function () {
            return {
                id: Math.floor(Math.random() * 1000000),

                opened: this.show as boolean,
                direction: this.show as boolean,

                icon: null as HTMLElement | null,
            };
        },

        mounted: function (): void {
            this.$nextTick(() => {
                this.addListeners();

                this.icon = document.getElementById(`icon-${this.elementID}`);

                // start the animation if the collapsibe starts closed (which is NOT the default)
                this.startAnimation();
            });
        },

        methods: {
            addListeners: function (): void {
                document.getElementById(this.elementID)?.addEventListener("show.bs.collapse", () => {
                    this.direction = true;
                    this.startAnimation();
                });

                document.getElementById(this.elementID)?.addEventListener("hide.bs.collapse", () => {
                    this.direction = false;
                    this.startAnimation();
                });
            },

            startAnimation: function (): void {
                if (this.icon == null) {
                    return console.warn(`Cannot animate on ${this.elementID}, icon is null`);
                }

                let start: number = 0;
                let prev: number = 0;

                const step = (timestamp: number) => {
                    if (start == 0) {
                        start = timestamp;
                    }

                    const elapsed = timestamp - start;

                    if (prev != timestamp) {
                        const count: number = Math.min(0.4 * elapsed, 90);
                        if (this.direction == true) {
                            this.icon!.style.transform = `rotate(${count - 90}deg)`;
                        } else {
                            this.icon!.style.transform = `rotate(-${count}deg)`;
                        }
                    }

                    if (elapsed < 500) {
                        prev = timestamp;
                        window.requestAnimationFrame(step);
                    }
                };

                window.requestAnimationFrame(step);
            },
        },

        computed: {
            elementID: function (): string {
                return `collapsible-${this.id}`;
            },

            classes: function (): string[] {
                return [this.SizeClass, this.BgColor, this.BgColor == "bg-light" ? "text-dark" : "text-light"];
            },
        },
    });
    export default Collapsible;
</script>
