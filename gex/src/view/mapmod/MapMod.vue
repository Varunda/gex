<template>
    <div id="mapmod-root">
        <div id="canvas-root"></div>
        <div id="overlay"></div>
        <div id="map-list">

            <select v-if="mapList.state == 'loaded'" v-model="mapFilename" class="form-select">
                <option :value="null">pick a map</option>
                <option v-for="map in mapListData" :key="map.fileName" :value="map.fileName">{{ map.name }}</option>
            </select>
            
        </div>
    </div>
</template>

<style>

    body {
        height: 100vh;
        margin: 0;
        display: flex;
        flex-direction: column;
    }

    #root {
        flex-grow: 1;
        flex-shrink: 1;
        flex-basis: 1;

        border: 1px red;
    }

    #overlay {
        position: absolute;
        top: 64px;
        width: 180px;
        z-index: 100;
        display: block;
        background: green;
    }

    #map-list {
        position: absolute;
        bottom: 16px;
        left: 8px;
    }

    .lil-gui {
        position: absolute;
        right: 10px;
    }

</style>

<script lang="ts">
    import Vue from "vue";
    import { Loadable, Loading } from "Loading";

    import { BarMapData, BarMapFileHeader } from "model/BarMapData";
    import { BarMap } from "model/BarMap";

    import { MapApi } from "api/MapApi";

    import * as three from "three";
    import { MapControls } from "three/examples/jsm/controls/MapControls";
    import Stats from "three/examples/jsm/libs/stats.module";
    import { GUI } from "three/examples/jsm/libs/lil-gui.module.min";
    import { Water } from "three/examples/jsm/objects/Water";

    const timer: three.Timer = new three.Timer();
    timer.connect(document);

    const stats = new Stats();

    const settings = {
        displacementScale: 10 as number,
        invert: false as boolean,
        sunIntensity: 1 as number,
        lightColor: 0xfffffff as number,
        showWater: true as boolean,
        waterHeight: 0 as number,
        waterDistortion: 3.7 as number,
        useNormal: true as boolean,
        useEmissive: true as boolean,
    };

    let overlay = document.getElementById("overlay");
    let camera: three.Camera | null = null;
    let renderer: three.WebGLRenderer | null = null;
    let controls: MapControls | null = null;
    let scene: three.Scene | null = null;
    let heightMap: three.Texture | null = null;
    let material: three.MeshStandardMaterial | null = null;
    let light: three.AmbientLight | null = null;
    let mapMesh: three.Mesh | null = null;
    let water: Water | null = null;
    let normalMap: three.Texture | null = null;
    let specularMap: three.Texture | null = null;

    function render(): void {
        stats.begin();
        timer.update();

        if (!overlay) {
            overlay = document.getElementById("overlay");
        }

        if (overlay && camera != null) {
            overlay.innerHTML = `rot ${camera.rotation.x.toFixed(2)} ${camera.rotation.y.toFixed(2)} ${camera.rotation.z.toFixed(2)}<br>`
            + `pos ${camera.position.x.toFixed(2)} ${camera.position.y.toFixed(2)} ${camera.position.z.toFixed(2)}`;
        }

        if (scene != null && camera != null && renderer != null) {
            renderer.render(scene, camera);
        }

        stats.end();
    }

    export const MapMod = Vue.extend({
        props: {

        },

        data: function() {
            return {
                mapFilename: "hooked_1.1.1" as string,
                mapData: Loadable.idle() as Loading<BarMapData>,

                mapList: Loadable.idle() as Loading<BarMap[]>,

                originalHeightMapData: [] as number[],
                heightMapData: [] as number[],
            }
        },

        mounted: function(): void {
            document.title = `Gex / Map view`;
            this.$nextTick(() => {
                this.loadMaps();
                this.makeThree();

                if (this.mapFilename != "") {
                    this.loadMapData();
                }
            });
        },

        methods: {

            loadMaps: async function(): Promise<void> {
                this.mapList = Loadable.loading();
                this.mapList = await MapApi.getAll();
            },

            makeThree: function(): void {
                const nav: HTMLElement | null = document.getElementById("nav");
                const root: HTMLElement | null = document.getElementById("root");

                if (nav == null || root == null) {
                    console.error(`missing nav or root, cannot make threejs`);
                    return;
                }

                root.appendChild(stats.dom);
                this.setupGui(root);

                scene = new three.Scene();
                camera = new three.PerspectiveCamera(75, root.clientWidth / root.clientHeight, 0.01, 2000000);

                renderer = new three.WebGLRenderer();
                renderer.setSize(root.clientWidth, root.clientHeight);
                renderer.setAnimationLoop(render);
                renderer.toneMapping = three.ACESFilmicToneMapping;
                renderer.toneMappingExposure = 0.9;
                root.append(renderer.domElement);

                controls = new MapControls(camera, renderer.domElement);
                controls.dampingFactor = 1;

                controls.addEventListener("change", () => {
                    //render();
                });

                camera.position.z = 5;
                camera.position.x = 5;
                camera.position.y = 5;

                const xAxis: three.Line = new three.Line(
                    new three.BufferGeometry().setFromPoints([ new three.Vector3(-1000, 0, 0), new three.Vector3(1000, 0, 0), ]),
                    new three.LineBasicMaterial( { color: 0xff0000 })
                );
                scene.add(xAxis);

                const yAxis: three.Line = new three.Line(
                    new three.BufferGeometry().setFromPoints([ new three.Vector3(0, -1000, 0), new three.Vector3(0, 1000, 0), ]),
                    new three.LineBasicMaterial( { color: 0x00ff00 })
                );
                scene.add(yAxis);

                const zAxis: three.Line = new three.Line(
                    new three.BufferGeometry().setFromPoints([ new three.Vector3(0, 0, -1000), new three.Vector3(0, 0, 1000), ]),
                    new three.LineBasicMaterial( { color: 0x0000ff })
                );
                scene.add(zAxis);

                light = new three.AmbientLight(0xffffff, 1);
                scene?.add(light);

                const waterGeo = new three.BoxGeometry(1, 0.01, 1);
                water = new Water(waterGeo, {
                    textureHeight: 512,
                    textureWidth: 512,
                    sunDirection: new three.Vector3(),
                    waterNormals: new three.TextureLoader().load('/img/waternormals.jpg', (texture) => {
                        texture.wrapS = texture.wrapT = three.RepeatWrapping;
                    }),
                    sunColor: 0xffffff,
                    waterColor: 0x006994cc,
                    distortionScale: 3.7
                });
                scene?.add(water);

                render();
            },

            loadMapData: async function(): Promise<void> {
                material?.dispose();
                material = null;

                if (mapMesh != null) {
                    scene?.remove(mapMesh);
                }
                mapMesh?.dispose();
                mapMesh = null;

                if (this.mapFilename == "") {
                    return;
                }

                this.mapData = Loadable.loading();
                this.mapData = await MapApi.getMapData(this.mapFilename);
                console.log(`MapMod> mapData request complete`);

                if (this.mapData.state != "loaded") {
                    return;
                }

                document.title = `Gex / Map view / ${this.mapData.data.name}`;

                const header: BarMapFileHeader = this.mapData.data.header;
                this.heightMapData = header.heightMap;
                this.originalHeightMapData = header.heightMap;

                const mapGeometry = new three.PlaneGeometry(header.width / 10, header.height / 10, header.width + 1, header.height + 1);
                mapGeometry.rotateX(-Math.PI / 2);

                if (water != null) {
                    water.scale.x = header.width / 10;
                    water.scale.z = header.height / 10;
                }

                const loader: three.TextureLoader = new three.TextureLoader();
                const url: string = `/api/map/${encodeURI(this.mapFilename)}`;
                const texture = await loader.loadAsync(url + "/texture");

                material = new three.MeshStandardMaterial({
                    side: three.DoubleSide,
                    map: texture,
                    displacementMap: heightMap,
                    displacementScale: 10,

                    normalMap: loader.load(url + "/normal"),
                    normalScale: new three.Vector2(1.5, 1.5),

                    emissiveMap: loader.load(url + "/specular"),
                    emissiveIntensity: 100,

                    metalness: 0.1,
                    roughness: 0.92
                });
                normalMap = material.normalMap;
                specularMap = material.emissiveMap;

                mapMesh = new three.Mesh(mapGeometry, material);
                scene?.add(mapMesh);
                console.log(`MapMod> rendering`);

                this.generateHeightmap();
            },

            setupGui: function(root: HTMLElement): void {
                const gui = new GUI({
                    container: root,
                    injectStyles: true
                });

                gui.add(settings, "displacementScale").min(1).max(100).onChange((value: number) => {
                    if (material != null) {
                        material.displacementScale = value;
                    }
                    render();
                });

                gui.add(settings, "sunIntensity").min(0.01).max(5).step(0.1).onChange((value: number) => {
                    if (light != null) {
                        light.intensity = value;
                    }
                    render();
                });

                gui.addColor(settings, "lightColor").onChange((value: number) => {
                    if (light != null) {
                        light.color = new three.Color(((value >> 16) & 0xFF) / 0xFF, ((value >> 8) & 0xFF) / 0xFF, (value & 0xFF) / 0xFF);
                    }
                    render();
                });

                gui.add(settings, "waterHeight").min(-1000).max(1000).onChange((value: number) => {
                    if (water != null) {
                        water.position.y = value / 100;
                    }
                    render();
                });

                gui.add(settings, "waterDistortion").min(0).max(100).onChange((value: number) => {
                    if (water != null) {
                        water.material.uniforms["distortionScale"].value = value;
                    }
                });

                gui.add(settings, "showWater").onChange((value: boolean) => {
                    if (water != null) {
                        water.visible = value;
                    }
                    render();
                });

                gui.add(settings, "useNormal").onChange((value: boolean) => {
                    if (material == null) {
                        return;
                    }

                    if (value == true) {
                        material.normalMap = normalMap;
                    } else {
                        material.normalMap = null;
                    }

                    material.needsUpdate = true;
                    render();
                });

                gui.add(settings, "useEmissive").onChange((value: boolean) => {
                    if (material == null) {
                        return;
                    }

                    if (value == true) {
                        material.emissiveMap = specularMap;
                    } else {
                        material.emissiveMap = null;
                    }

                    material.needsUpdate = true;
                    render();
                });
            },

            generateHeightmap: function(): void {
                if (material == null) { return; }
                if (this.mapData.state != "loaded") { return; }

                const header: BarMapFileHeader = this.mapData.data.header;

                const canvas: HTMLCanvasElement = document.createElement("canvas");
                canvas.width = header.width + 1;
                canvas.height = header.height + 1;

                const ctx: CanvasRenderingContext2D = canvas.getContext("2d")!;
                ctx.fillStyle = "#000000";
                ctx.fillRect(0, 0, canvas.width, canvas.height);

                const image: ImageData = ctx.getImageData(0, 0, canvas.width, canvas.height);
                const imageData: Uint8ClampedArray = image.data;

                for (let i = 0; i < this.originalHeightMapData.length; ++i) {
                    const byte = (this.originalHeightMapData[i] / 65536) * 255;

                    imageData[(i * 4) + 0] = Math.min(255, byte);
                    imageData[(i * 4) + 1] = Math.min(255, byte);
                    imageData[(i * 4) + 2] = Math.min(255, byte);
                    imageData[(i * 4) + 3] = 0xFF;
                }

                ctx.putImageData(image, 0, 0);
                console.log(`MapMod> height map built`);

                heightMap = new three.CanvasTexture(canvas);
                heightMap.wrapS = three.ClampToEdgeWrapping;
                heightMap.wrapT = three.ClampToEdgeWrapping;
                heightMap.needsUpdate = true;

                material.displacementMap = heightMap;
                material.needsUpdate = true;
                //render();
            }

        },

        computed: {

            mapListData: function(): BarMap[] {
                if (this.mapList.state != "loaded") {
                    return [];
                }

                return [...this.mapList.data].sort((a, b) => {
                    return a.name.localeCompare(b.name);
                });
            }

        },

        watch: {
            "mapFilename": function(): void {
                this.loadMapData();
            }
        },

        components: {

        }
    });
    export default MapMod;
</script>