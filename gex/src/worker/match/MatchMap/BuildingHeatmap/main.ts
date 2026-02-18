import * as d3 from "d3";
import * as d3s from "d3-scale";
import * as d3z from "d3-zoom";
import "d3-contour";
import "d3-color";

import { BarMatchPlayer } from "model/BarMatchPlayer";

onmessage = (ev) => {
    const locs: [number, number][] = ev.data[0];
    const player: BarMatchPlayer = ev.data[1]
    const imgW: number = ev.data[2];
    const imgH: number = ev.data[3];
    const mapW: number = ev.data[4];
    const mapH: number = ev.data[5];

    const density = d3.contourDensity()
        .x((d) => toImgX(d[0]))
        .y((d) => toImgZ(d[1]));

    function toImgX(x: number): number { return x / mapW * imgW; };
    function toImgZ(z: number): number { return z / mapH * imgH; };

    console.time(`match-map: building headmap density`);
    const heatmap = density.size([imgW, imgH])
        .bandwidth(30)(locs);

    console.timeEnd(`match-map: building headmap density`);

    postMessage([heatmap, player]);
};