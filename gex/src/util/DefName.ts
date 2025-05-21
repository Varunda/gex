
export class DefNameUtil {

    public static getName(defName: string): string {
        if (defName == "armap") {
            return "Armada Air Lab";
        } else if (defName == "armfhp") {
            return "Armada Naval Hover Lab";
        } else if (defName == "armhp") {
            return "Armada Hover Lab";
        } else if (defName == "armlab") {
            return "Armada Bot Lab";
        } else if (defName == "armsy") {
            return "Armada Naval Lab";
        } else if (defName == "armvp") {
            return "Armada Vehicle Lab";
        } else if (defName == "corap") {
            return "Cortex Air Lab";
        } else if (defName == "corfhp") {
            return "Cortex Naval Hover Lab";
        } else if (defName == "corhp") {
            return "Cortex Hover Lab";
        } else if (defName == "corlab") {
            return "Cortex Bot Lab";
        } else if (defName == "corsy") {
            return "Cortex Naval Lab";
        } else if (defName == "corvp") {
            return "Cortex Vehicle Lab";
        } else if (defName == "legap") {
            return "Legion Air Lab";
        } else if (defName == "legfhp") {
            return "Legion Naval Hover Lab";
        } else if (defName == "leghp") {
            return "Legion Hover Lab";
        } else if (defName == "leglab") {
            return "Legion Bot Lab";
        } else if (defName == "legsy") {
            return "Legion Naval Lab";
        } else if (defName == "legvp") {
            return "Legion Vehicle Lab";
        } else {
            return `<unknown defname ${defName}>`;
        }
    }

}