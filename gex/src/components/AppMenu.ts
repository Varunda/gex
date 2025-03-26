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
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                            pages
                        </a>
                        <ul class="dropdown-menu">
                            <li><a class="dropdown-item" href="/">Recent games</a></li>
                            <li><a class="dropdown-item" href="/legal">Legal</a></li>
                        </ul>
                    </li>
                </ul>

                <ul class="navbar-nav ms-auto">
                    <li class="nav-item dropdown">
                        <a class="nav-link dropdown-toggle" href="#" role="button" data-bs-toggle="dropdown">
                        hi
                        </a>
                        <ul class="dropdown-menu dropdown-menu-end">
                            <li><a class="dropdown-item" href="/settings">settings</a></li>
                            <li><a class="dropdown-item" href="/posts">logout</a></li>
                        </ul>
                    </li>
                </ul>

            </div>
        </nav>
    `,
});
