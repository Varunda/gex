using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Layout;
using gex.Coven.Table.Controls;
using System;

namespace gex.Coven.Table.Presenters;

internal static class CovenTableViewLayoutHelper {
    /// <summary>
    /// Distributes <paramref name="availableWidth"/> among the columns.
    /// Pixel columns take their fixed size; remaining space is split proportionally among star columns.
    /// Auto is treated as 1*.
    /// </summary>
    public static bool UpdateActualWidths(
        AvaloniaList<CovenTableViewColumn> columns,
        double availableWidth,
        bool useLayoutRounding,
        double layoutScale) {
        if (columns.Count == 0) {
            return false;
        }

        double fixedTotal = 0.0;
        double starTotal = 0.0;
        bool modified = false;

        for (int i = 0; i < columns.Count; i++) {
            if (!columns[i].IsVisible) {
                if (columns[i].ActualWidth != 0) {
                    columns[i].ActualWidth = 0;
                    modified = true;
                }
                continue;
            }

            GridLength width = columns[i].Width;
            if (width.IsAbsolute) {
                double actualWidth = width.Value;

                if (useLayoutRounding) {
                    actualWidth = LayoutHelper.RoundLayoutValue(actualWidth, layoutScale);
                }

                if (columns[i].ActualWidth != actualWidth) {
                    columns[i].ActualWidth = actualWidth;
                    modified = true;
                }

                fixedTotal += actualWidth;
            } else {
                // Star or Auto — treat both as star
                starTotal += width.IsStar ? width.Value : 1.0;
            }
        }

        double starBudget;

        if (double.IsPositiveInfinity(availableWidth)) {
            // The headers aren't supposed to be measured with infinity, as they're normally outside the main ScrollViewer.
            // If they've been relocated, or are missing, use 1000 pixels arbitrarily so star columns are still displayed.
            starBudget = 1000;
        } else {
            starBudget = Math.Max(0.0, availableWidth - fixedTotal);
        }

        // Distribute the star budget by rounding the cumulative right edge of each star column rather than each width in
        // isolation. This spreads the sub-pixel remainders across the columns instead of letting them accumulate, so the
        // sum of the rounded star widths equals the rounded star budget and the final layout matches the available width.
        double unroundedEdge = 0.0;
        double roundedEdge = 0.0;

        for (int i = 0; i < columns.Count; i++) {
            if (!columns[i].IsVisible) {
                continue;
            }

            GridLength width = columns[i].Width;
            if (!width.IsAbsolute) {
                double share = width.IsStar ? width.Value : 1.0;
                unroundedEdge += starTotal > 0 ? share / starTotal * starBudget : 0;

                double actualWidth = unroundedEdge - roundedEdge;

                if (useLayoutRounding) {
                    double nextRoundedEdge = LayoutHelper.RoundLayoutValue(unroundedEdge, layoutScale);
                    actualWidth = nextRoundedEdge - roundedEdge;
                    roundedEdge = nextRoundedEdge;
                } else {
                    roundedEdge = unroundedEdge;
                }

                if (columns[i].ActualWidth != actualWidth) {
                    columns[i].ActualWidth = actualWidth;
                    modified = true;
                }
            }
        }

        return modified;
    }

    /// <remarks>
    /// Contract between <see cref="UpdateActualWidths"/>, <see cref="NeedsActualWidths"/> and <see cref="ResetActualWidths"/>: <br/>
    /// If <see cref="TableViewColumn.ActualWidth"/> is NaN, a recalculation of the ActualWidths is needed.
    /// All column widths are reset and recalculated together, so checking the first one is sufficient.
    /// </remarks>
    public static bool NeedsActualWidths(AvaloniaList<CovenTableViewColumn> columns)
        => columns.Count > 0 && double.IsNaN(columns[0].ActualWidth);

    public static void ResetActualWidths(AvaloniaList<CovenTableViewColumn> columns) {
        foreach (CovenTableViewColumn column in columns) {
            column.ActualWidth = double.NaN;
        }
    }

    public static Size MeasureRow(AvaloniaList<CovenTableViewColumn> columns, Avalonia.Controls.Controls cells, Size availableSize) {
        if (cells.Count != columns.Count) {
            return default;
        }

        double totalWidth = 0.0;
        double totalHeight = 0.0;

        for (int i = 0; i < cells.Count; i++) {
            if (!columns[i].IsVisible) {
                continue;
            }

            Control child = cells[i];
            double columnWidth = columns[i].ActualWidth;
            child.Measure(new Size(columnWidth, availableSize.Height));
            totalWidth += columnWidth;
            totalHeight = Math.Max(totalHeight, child.DesiredSize.Height);
        }

        return new Size(totalWidth, totalHeight);
    }

    public static Size ArrangeRow(AvaloniaList<CovenTableViewColumn> columns, Avalonia.Controls.Controls cells, Size finalSize, double offset) {
        if (cells.Count != columns.Count) {
            return finalSize;
        }

        double x = offset;
        for (int i = 0; i < cells.Count; i++) {
            if (!columns[i].IsVisible) {
                continue;
            }

            double width = columns[i].ActualWidth;
            cells[i].Arrange(new Rect(x, 0, width, finalSize.Height));
            x += width;
        }

        return finalSize;
    }
}
