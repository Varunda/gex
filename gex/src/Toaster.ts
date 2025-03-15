import * as bootstrap from "bootstrap";

import { Loading, Loadable } from "Loading";

export type TextColor = "primary" | "secondary" | "success" | "danger" | "warning" | "info" | "body" | "muted";

export default class Toaster {

    private static _nextInstance: number = 1;

    public static remove(instanceID: number): void {
        console.log(`Removing toast instance ${instanceID}`);
        document.getElementById(`toast-entry-${instanceID}`)?.remove();
    }

    public static add(headerText: string, bodyText: string, headerColor: TextColor = "body"): void {
        const instId: number = Toaster._nextInstance++;

        const elem: HTMLElement | null = document.getElementById("toaster");
        if (elem == null) {
            console.error(`failed to find #toaster for toaster component`);
            return;
        }

        const toast = bootstrap.Toast.getOrCreateInstance(elem);

        const html: string = `
                <div class="toast-header" style="line-height: 1.75;" data-bs-dismiss="toast">
                    <strong class="me-auto text-${headerColor}">${headerText}</strong>
                    <button type="button" class="ml-2 mb-1 btn-close" data-bs-dismiss="toast">
                        <span>&times;</span>
                    </button>
                </div>
                <div class="toast-body">
                    ${bodyText}
                </div>`;

        const inner = document.createElement("div");
        inner.classList.add("toast", "m-3", "show");
        inner.dataset["bsDelay"] = "8000";
        inner.id = `toast-entry-${instId}`;
        inner.addEventListener("click", (_) => {
            Toaster.remove(instId);
        });
        inner.innerHTML = html;

        elem.appendChild(inner);

        setTimeout(() => {
            Toaster.remove(instId);
        }, 8 * 1000);

        toast.show();
    }

}

(window as any).Toaster = Toaster; // Necessary so they can be removed

