using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.LogicalTree;
using gex.Coven.Table.Presenters;
using System.Diagnostics;

namespace gex.Coven.Table.Controls;

/// <summary>
/// A row container in a <see cref="TableView"/>.
/// </summary>
[TemplatePart(PartCellsPresenter, typeof(CovenTableViewCellsPresenter))]
public class CovenTableViewRow : ListBoxItem {
    private const string PartCellsPresenter = "PART_CellsPresenter";

    private CovenTableViewCellsPresenter? _cellsPresenter;

    internal AvaloniaList<CovenTableViewColumn>? Columns { get; set; }

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        if (_cellsPresenter is not null) {
            Debug.Assert(_cellsPresenter.Row == this);
            _cellsPresenter.RemoveCells();
            _cellsPresenter.Row = null;
        }

        _cellsPresenter = e.NameScope.Find<CovenTableViewCellsPresenter>(PartCellsPresenter);

        if (_cellsPresenter is not null) {
            Debug.Assert(_cellsPresenter.Row is null);
            _cellsPresenter.Row = this;
            _cellsPresenter.RebuildCells();
        }
    }

    internal void ClearCells() => _cellsPresenter?.ClearCells();

    internal void InvalidateCellsMeasure() => _cellsPresenter?.InvalidateMeasure();

    internal void RebuildCells() => _cellsPresenter?.RebuildCells();

    internal void RefreshCell(int columnIndex) => _cellsPresenter?.RefreshCell(columnIndex);

    internal IAvaloniaList<ILogical> GetLogicalChildren() => LogicalChildren;

}
