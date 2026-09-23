import * as d3 from "d3";
import * as d3s from "d3-scale";
import * as d3z from "d3-zoom";
import "d3-contour";
import "d3-color";

import { BarMatchTeam } from "model/BarMatchTeam";
import { ArmyValuePosition } from "view/match/compute/ArmyValuePosition";

//
onmessage = (ev) => {
    const locs: ArmyValuePosition[] = ev.data[0];
    const team: BarMatchTeam = ev.data[1]
    const imgW: number = ev.data[2];
    const imgH: number = ev.data[3];
    const mapW: number = ev.data[4];
    const mapH: number = ev.data[5];

    const density = d3.contourDensity<[number, number, number]>()
        .x((d) => toImgX(d[0]))
        .y((d) => toImgZ(d[1]))
        .weight((d) => d[2]);

    function toImgX(x: number): number { return x / mapW * imgW; };
    function toImgZ(z: number): number { return z / mapH * imgH; };

    console.time(`match-map: building army value position density`);

    const heatmaps: d3.ContourMultiPolygon[][] = [];
    for (const pos of locs) {
        const heatmap = density.size([imgW, imgH])
            .bandwidth(30)(pos.entries.map(iter => {
                return [iter.x, iter.z, iter.metalValue];
            })
        );
        heatmaps.push(heatmap);
    }

    console.timeEnd(`match-map: building headmap density`);

    postMessage([heatmaps, team]);
};