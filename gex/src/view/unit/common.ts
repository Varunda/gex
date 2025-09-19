import { ApiBarUnit, BarUnit, BarUnitWeapon } from "model/BarUnit";

export type CalcBarWeapon = BarUnitWeapon & {
    
};

export type CalcBarUnit = BarUnit & {
    energyRepay: number;
    metalRepay: number;
};

export type CalcApiBarUnit = ApiBarUnit & {
    calcUnit: CalcBarUnit
};