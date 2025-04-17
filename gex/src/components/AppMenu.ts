import Vue from "vue";

export const GexMenu = Vue.extend({
    template: `
        <nav class="navbar navbar-expand bg-body-tertiary pt-0 mb-2 container-fluid-negative px-3">
            <div class="navbar-collapse">
                <a class="navbar-brand py-0" href="/">
                    <img src="/img/logo0.png" style="height: 100%; width: 48px;" title="homepage" />
                    Gex
                </a>
                <ul class="navbar-nav">
                    <li class="nav-item">
                        <a class="nav-link" href="/">Recent games</a>
                    </li>
                    <li class="nav-item">
                        <a class="nav-link" href="/users">User search</a>
                    </li>
                </ul>

                <ul class="navbar-nav ms-auto">
                    <li class="nav-item">
                        <a class="nav-link" href="/legal">Legal</a>
                    </li>
                </ul>

            </div>
        </nav>
    `,
});
