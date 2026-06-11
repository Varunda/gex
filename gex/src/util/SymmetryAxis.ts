
export default class SymmetryAxis {

    public static MISSING: number = 0;

    public static MIRRORED_VERTICAL: number = 1;

    public static MIRRORED_HORIZONTAL: number = 2;

    public static MIRRORED_DIAGONAL: number = 3;

    public static FLIPPED_VERTICAL: number = 4;

    public static FLIPPED_HORIZONTAL: number = 5;

    public static getName(axis: number): string {
        switch (axis) {
            case SymmetryAxis.MISSING: return "missing";
            case SymmetryAxis.MIRRORED_VERTICAL: return "mirrored vertical";
            case SymmetryAxis.MIRRORED_HORIZONTAL: return "mirrored horizontal";
            case SymmetryAxis.MIRRORED_DIAGONAL: return "mirrored diagonal";
            case SymmetryAxis.FLIPPED_VERTICAL: return "flipped vertical";
            case SymmetryAxis.FLIPPED_HORIZONTAL: return "flipped horizontal";
        }

        return `unchecked ${axis}`;
    }

}