import LocaleUtil from "./Locale";


export default class CompactUtils {

    private static over1k: Intl.NumberFormat | undefined = undefined;
    private static under1k: Intl.NumberFormat | undefined = undefined;

    public static compact(value: number): string {
        if (CompactUtils.over1k == undefined)  {
            CompactUtils.over1k = Intl.NumberFormat(undefined, {
                notation: "compact",
                minimumFractionDigits: 1,
                maximumFractionDigits: 1
            }) as Intl.NumberFormat;
        }
        if (CompactUtils.under1k == undefined) {
            CompactUtils.under1k = Intl.NumberFormat(undefined, {
                notation: "compact",
                maximumFractionDigits: 0,
                minimumFractionDigits: 0
            });
        }

        // we cannot have 1.3k and 3 using the same number format
        // so, use a different if the value is over/under 1k
        if (value > 999) {
            return LocaleUtil.format(value, CompactUtils.over1k);
        } else {
            return LocaleUtil.format(value, CompactUtils.under1k);
        }
    }

}