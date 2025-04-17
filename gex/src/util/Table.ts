import TimeUtils from "util/Time";
import CompactUtils from "./Compact";
import LocaleUtil from "./Locale";
export default class TableUtils {

    public static defaultValueFormatter(v: number): string {
        if (Math.abs(v) <= 1) {
            return LocaleUtil.locale(v, 2);
        } else if (Math.abs(v) < 100) {
            return LocaleUtil.locale(v, 0);
        }

        return CompactUtils.compact(v);
    }

    public static defaultLabelFormatter(label: string): string {
        return label;
    }

    public static chart(elementName: string, context: any,
        valueFormatter: ((_: number) => string) | null,
        labelFormatter: ((_: string) => string) | null = null
    ) {

        labelFormatter ??= TableUtils.defaultLabelFormatter;

        // tooltip Element
        let tooltipEl: HTMLElement | null = document.getElementById(elementName);

        // create element on first render
        if (!tooltipEl) {
            tooltipEl = document.createElement('div');
            tooltipEl.classList.add("border", "border-light");
            tooltipEl.id = elementName;
            tooltipEl.innerHTML = '<table class="table table-sm p-1 mb-0"></table>';
            document.body.appendChild(tooltipEl);
        }

        // hide if no tooltip
        const tooltipModel = context.tooltip;
        if (tooltipModel.opacity === 0) {
            tooltipEl.style.opacity = "0";
            return;
        }

        // set caret Position
        tooltipEl.classList.remove('above', 'below', 'no-transform');
        if (tooltipModel.yAlign) {
            tooltipEl.classList.add(tooltipModel.yAlign);
        } else {
            tooltipEl.classList.add('no-transform');
        }

        // set Text
        if (tooltipModel.body) {
            const titleLines = tooltipModel.title || [];

            // find the closest point and save the index,
            //      which is used to bold the line of the tooltip that has the line closest to
            let closestDatasetIndexs: number[] = [];
            let closestDist: number = Number.MAX_VALUE;
            for (let i = 0; i < tooltipModel.dataPoints.length; ++i) {
                const point = tooltipModel.dataPoints[i];
                const pointy = point.element.y;
                let diff = Math.abs(tooltipModel.caretY - pointy);
                //console.log(`diff ${diff}, dist ${closestDist}, iter ${point.datasetIndex}, index ${i}, y ${tooltipModel.caretY}, cp1y ${pointy}`);
                if (diff < closestDist) {
                    closestDatasetIndexs = [];
                    closestDatasetIndexs.push(point.datasetIndex);
                    closestDist = diff;
                }
                if (diff == closestDist) {
                    closestDatasetIndexs.push(point.datasetIndex);
                }
            }

            let innerHtml = '<thead>';

            titleLines.forEach((title: string) => {
                innerHtml += '<tr class="th-border-top-0"><th style="border-bottom: 0" colspan="2">' + title + '</th></tr>';
            });
            innerHtml += '</thead><tbody>';

            // sort the values from descending to ascending
            [...tooltipModel.dataPoints].sort((a, b) => {
                return b.parsed.y - a.parsed.y;
            }).forEach((value, i) => {
                if (closestDatasetIndexs.indexOf(value.datasetIndex) > -1) {
                    innerHtml += `<tr style="color: var(--bs-black); font-weight: 600">`;
                } else {
                    innerHtml += `<tr style="color: var(--bs-dark); font-weight: 400">`;
                }

                innerHtml += `<td><span style="color: ${value.dataset.borderColor}">&#9632;</span>${labelFormatter(value.dataset.label)}&nbsp;</td>`;
                innerHtml += `<td>${valueFormatter ? valueFormatter(value.parsed.y) : value.parsed.y}</td>`;
                innerHtml += `</tr>`;
            });

            innerHtml += '</tbody>';

            let tableRoot = tooltipEl.querySelector('table');
            if (tableRoot == null) {
                throw `failed to find table element`;
            }
            tableRoot.innerHTML = innerHtml;
        }

        const canvasYOffset: number = context.chart.canvas.offsetTop;
        const position = context.chart.canvas.getBoundingClientRect();
        const tooltipPos = tooltipEl.getBoundingClientRect();
        const vp: number = window.visualViewport?.height ?? 0;
        //console.log(position, "win x", window.pageXOffset, "win y", window.pageYOffset, "tt x", tooltipModel.caretX, "tt y", tooltipModel.caretY, "padding", tooltipModel.padding, "tt pos", tooltipPos, "vp h", vp);

        // Display, position, and set styles for font
        tooltipEl.style.opacity = "1";
        tooltipEl.style.position = "absolute";
        if ((tooltipModel.caretX / position.width) < 0.5 || true) {
            tooltipEl.style.left = position.left + window.pageXOffset + tooltipModel.caretX + "px";
            tooltipEl.style.right = "";
        } else {
            //tooltipEl.style.left = "";
            // this is like 15px off and idk why and i've spent like 2 hours here now and i don't care
            //tooltipEl.style.right = (position.right - tooltipModel.caretX - position.x) + "px"; // - window.pageXOffset + position.left) + 'px';
        }

        if ((tooltipModel.caretY + tooltipPos.height + canvasYOffset - window.pageYOffset - vp) > 15) {
            //console.log(`this would go off screen!`);
            tooltipEl.style.top = (position.top + window.pageYOffset) + "px";
        }  else {
            tooltipEl.style.top = position.top + window.pageYOffset + tooltipModel.caretY + "px";
        }
        
        tooltipEl.style.fontFamily = "Atkinson Hyperlegible";
        //tooltipEl.style.padding = tooltipModel.padding + "px " + tooltipModel.padding + "px";
        tooltipEl.style.pointerEvents = "none";
    }

}
