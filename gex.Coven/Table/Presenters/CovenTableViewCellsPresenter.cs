using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using gex.Coven.Table.Controls;
using static gex.Coven.Table.Presenters.CovenTableViewLayoutHelper;

namespace gex.Coven.Table.Presenters;

/// <summary>
/// Lays out the cells of a <see cref="TableViewRow"/> according to the column definitions
/// of the parent <see cref="TableView"/>.
/// </summary>
/// <remarks>
/// The cells are recycled alongside their owning row in a <see cref="VirtualizingStackPanel"/>,
/// but are not virtualized in a single row (i.e. there is no column virtualization).
/// </remarks>
public class CovenTableViewCellsPresenter : Panel {

    internal CovenTableViewRow? Row { get; set; }

    internal void ClearCells() {
        foreach (Control child in Children) {
            if (child is CovenTableViewCell cell) {
                cell.Column = null;
            }
        }
    }

    internal void RemoveCells() {
        ClearCells();
        Children.Clear();
        Row?.GetLogicalChildren().Clear();
    }

    internal void RebuildCells() {
        if (Row?.Columns is not { } columns) {
            RemoveCells();
            return;
        }

        if (columns.Count != Children.Count) {
            RemoveCells();

            foreach (CovenTableViewColumn column in columns) {
                CovenTableViewCell cell = new CovenTableViewCell { Column = column };
                Children.Add(cell);
            }

            Row.GetLogicalChildren().AddRange(Children);
        } else {
            for (int i = 0; i < columns.Count; i++) {
                ((CovenTableViewCell)Children[i]).Column = columns[i];
            }
        }

        InvalidateMeasure();
    }

    internal void RefreshCell(int columnIndex) => ((CovenTableViewCell)Children[columnIndex]).Refresh();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) {
        if (Row?.Columns is not { } columns) {
            return default;
        }

        // In a standard template, the column widths should have been computed by the headers' presenter.
        // If for some reason they weren't, do it now.
        if (NeedsActualWidths(columns)) {
            UpdateActualWidths(columns, availableSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this));
        }

        return MeasureRow(columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize) {
        if (Row?.Columns is not { } columns) {
            return finalSize;
        }

        if (NeedsActualWidths(columns)) {
            UpdateActualWidths(columns, finalSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this));
        }

        return ArrangeRow(columns, Children, finalSize, 0);
    }
}
