using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.LogicalTree;
using gex.Coven.Table.Controls;
using System.ComponentModel;
using static gex.Coven.Table.Presenters.CovenTableViewLayoutHelper;

namespace gex.Coven.Table.Presenters;

/// <summary>
/// Displays the column headers for a <see cref="TableView"/>.
/// Computes column widths (pixel and star) and exposes them via
/// <see cref="TableViewColumn.ActualWidth"/> so that <see cref="TableViewCellsPresenter"/>
/// can align cells with the headers.
/// </summary>
public class CovenTableViewColumnHeadersPresenter : Panel {

    internal CovenTableView? TableView { get; set; }

    internal void RebuildHeaders() {
        Children.Clear();

        if (TableView is null) {
            return;
        }

        foreach (CovenTableViewColumn column in TableView.Columns) {
            CovenTableViewColumnHeader header = new CovenTableViewColumnHeader { Column = column };
            Children.Add(header);
        }

        InvalidateMeasure();
    }

    /// <inheritdoc />
    protected override void OnAttachedToLogicalTree(LogicalTreeAttachmentEventArgs e) {
        base.OnAttachedToLogicalTree(e);

        TableView = this.FindLogicalAncestorOfType<CovenTableView>();

        RebuildHeaders();

        if (TableView is not null) {
            TableView.HeadersPresenter = this;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged) {
                notifyPropertyChanged.PropertyChanged += OnScrollPropertyChanged;
            }
        }
    }

    protected override void OnDetachedFromLogicalTree(LogicalTreeAttachmentEventArgs e) {
        if (TableView is not null) {
            TableView.HeadersPresenter = null;

            if (TableView.Scroll is INotifyPropertyChanged notifyPropertyChanged) {
                notifyPropertyChanged.PropertyChanged -= OnScrollPropertyChanged;
            }

            TableView = null;
            RebuildHeaders();
        }

        base.OnDetachedFromLogicalTree(e);
    }

    private void OnScrollPropertyChanged(object? sender, PropertyChangedEventArgs e) {
        if (e.PropertyName == nameof(IScrollable.Offset)) {
            InvalidateArrange();
        }
    }

    internal void RefreshHeader(int columnIndex) => ((CovenTableViewColumnHeader)Children[columnIndex]).Refresh();

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize) {
        if (TableView is null) {
            return default;
        }

        if (UpdateActualWidths(TableView.Columns, availableSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this))) {
            TableView.InvalidateCellsMeasure();
        }

        return MeasureRow(TableView.Columns, Children, availableSize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize) {
        if (TableView is null) {
            return finalSize;
        }

        AvaloniaList<CovenTableViewColumn> columns = TableView.Columns;

        if (UpdateActualWidths(columns, finalSize.Width, UseLayoutRounding, LayoutHelper.GetLayoutScale(this))) {
            TableView.InvalidateCellsMeasure();
        }

        double offset = TableView.Scroll?.Offset.X ?? 0;
        return ArrangeRow(columns, Children, finalSize, -offset);
    }

}
