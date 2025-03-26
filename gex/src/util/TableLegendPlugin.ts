import { ChartType, Plugin } from "node_modules/chart.js/types/index.esm";

declare module "chart.js" {
    interface PluginOptionsByType<TType extends ChartType> {
        'html-legend':{
            containerID?: string
        }
    }
}