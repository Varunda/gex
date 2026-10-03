using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Metadata;
using gex.Coven.Table.Presenters;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using static gex.Coven.Table.Presenters.CovenTableViewLayoutHelper;

namespace gex.Coven.Table.Controls;

/// <summary>
/// A read-only tabular control that presents items in configurable columns.
/// </summary>
public class CovenTableView : ListBox {

    /// <summary>
    /// Defines the <see cref="Columns"/> property.
    /// </summary>
    public static readonly DirectProperty<CovenTableView, AvaloniaList<CovenTableViewColumn>?> ColumnsProperty =
        AvaloniaProperty.RegisterDirect<CovenTableView, AvaloniaList<CovenTableViewColumn>?>(
            nameof(Columns),
            o => o.Columns,
            (o, v) => o.Columns = v
        );

    /// <summary>
    /// Defines the <see cref="CanUserResizeColumns"/> property.
    /// </summary>
    public static readonly StyledProperty<bool> CanUserResizeColumnsProperty =
        AvaloniaProperty.Register<CovenTableView, bool>(nameof(CanUserResizeColumns), true);

    private IDisposable? _columnsSubscription;
    private AvaloniaList<CovenTableViewColumn>? _columns;

    [Obsolete(
        $"{nameof(ItemsControl.DisplayMemberBinding)} has no effect on a {nameof(CovenTableView)}. " +
        $"Use {nameof(CovenTableViewColumn)}.{nameof(CovenTableViewColumn.Binding)} instead.")]
    [SuppressMessage("AvaloniaProperty", "AVP1012", Justification = "Calls the base")]
    [AssignBinding]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public new BindingBase? DisplayMemberBinding {
        get => base.DisplayMemberBinding;
        set => base.DisplayMemberBinding = value;
    }

    [Obsolete(
        $"{nameof(ItemsControl.ItemTemplate)} has no effect on a {nameof(CovenTableView)}. " +
        $"Use {nameof(CovenTableViewColumn)}.{nameof(CovenTableViewColumn.CellTemplate)} instead.")]
    [SuppressMessage("AvaloniaProperty", "AVP1012", Justification = "Calls the base")]
    [InheritDataTypeFromItems(nameof(ItemsSource))]
    public new IDataTemplate? ItemTemplate {
        get => base.ItemTemplate;
        set => base.ItemTemplate = value;
    }

    /// <summary>
    /// Gets or sets the collection of columns displayed by this <see cref="TableView"/>.
    /// </summary>
    [NotNull]
    public AvaloniaList<CovenTableViewColumn>? Columns {
        get {
            if (_columns is null) {
                _columns = new AvaloniaList<CovenTableViewColumn>() { ResetBehavior = ResetBehavior.Remove };
                if (IsInitialized) {
                    SubscribeToColumns(_columns);
                }
            }

            return _columns;
        }

        set {
            AvaloniaList<CovenTableViewColumn>? oldValue = _columns;
            if (oldValue == value) {
                return;
            }

            UnsubscribeFromColumns();
            if (value != null) {
                value.ResetBehavior = ResetBehavior.Remove;
            }
            SetAndRaise(ColumnsProperty, ref _columns, value);

            if (IsInitialized) {
                if (value is not null) {
                    SubscribeToColumns(value);
                }

                RebuildHeaders();
                RebuildCells();
            }
        }
    }

    /// <summary>
    /// Gets or sets a value indicating whether the user can resize columns by dragging the
    /// separator between column headers.
    /// </summary>
    public bool CanUserResizeColumns {
        get => GetValue(CanUserResizeColumnsProperty);
        set => SetValue(CanUserResizeColumnsProperty, value);
    }

    internal CovenTableViewColumnHeadersPresenter? HeadersPresenter { get; set; }

    private void SubscribeToColumns(AvaloniaList<CovenTableViewColumn> columns) {
        Debug.Assert(_columnsSubscription is null);

        bool isInitialIteration = true;

        _columnsSubscription = columns.ForEachItem(
            added: column => {
                AttachColumn(column);
                if (!isInitialIteration) {
                    OnColumnsChanged();
                }
            },

            removed: column => {
                DetachColumn(column);
                OnColumnsChanged();
            },

            reset: () => { }
        );

        isInitialIteration = false;
    }

    private void AttachColumn(CovenTableViewColumn column) {
        if (column.TableView is not null && column.TableView != this) {
            throw new InvalidOperationException(
                $"The column {column} is already attached to a {nameof(CovenTableView)}.");
        }

        // Resolve styles and bindings before enabling refresh notifications. The headers
        // and cells are rebuilt after attachment to apply the column's current values.
        ((ISetLogicalParent)column).SetParent(this);
        column.TableView = this;
    }

    private void DetachColumn(CovenTableViewColumn column) {
        Debug.Assert(column.TableView == this || column.TableView is null);

        column.TableView = null;
        ((ISetLogicalParent)column).SetParent(null);
        column.ActualWidth = double.NaN;
    }

    private void UnsubscribeFromColumns() {
        if (_columnsSubscription is null) {
            return;
        }

        _columnsSubscription.Dispose();
        _columnsSubscription = null;

        foreach (CovenTableViewColumn column in Columns) {
            DetachColumn(column);
        }
    }

    /// <inheritdoc/>
    protected internal new Control CreateContainerForItemOverride(object? item, int index, object? recycleKey) => new CovenTableViewRow();

    /// <inheritdoc/>
    protected internal new void PrepareContainerForItemOverride(Control container, object? item, int index) {
        base.PrepareContainerForItemOverride(container, item, index);

        if (container is CovenTableViewRow row) {
            row.Columns = Columns;
            row.RebuildCells();
        }
    }

    /// <inheritdoc/>
    protected internal new bool NeedsContainerOverride(object? item, int index, out object? recycleKey)
        => NeedsContainer<CovenTableViewRow>(item, out recycleKey);

    /// <inheritdoc/>
    protected internal new void ClearContainerForItemOverride(Control element) {
        base.ClearContainerForItemOverride(element);

        if (element is CovenTableViewRow row) {
            row.Columns = null;
            row.ClearCells();
        }
    }

    private void OnColumnsChanged() {
        ResetActualWidths(Columns);
        RebuildHeaders();
        RebuildCells();
    }

    internal void OnColumnsSizeChanged() {
        ResetActualWidths(Columns);
        InvalidateHeadersMeasure();
        InvalidateCellsMeasure();
    }

    /// <inheritdoc />
    protected override void OnInitialized() {
        base.OnInitialized();

        if (_columns is not null) {
            SubscribeToColumns(_columns);
        }
    }

    private void RebuildHeaders()
        => HeadersPresenter?.RebuildHeaders();

    private void RebuildCells() {
        AvaloniaList<CovenTableViewColumn> columns = Columns;

        foreach (Control row in GetRealizedContainers()) {
            if (row is CovenTableViewRow tableViewRow) {
                tableViewRow.Columns = columns;
                tableViewRow.RebuildCells();
            } else {
                row.InvalidateMeasure();
            }
        }
    }

    internal void InvalidateHeadersMeasure()
        => HeadersPresenter?.InvalidateMeasure();

    internal void InvalidateCellsMeasure() {
        foreach (Control row in GetRealizedContainers()) {
            if (row is CovenTableViewRow tableViewRow) {
                tableViewRow.InvalidateCellsMeasure();
            } else {
                row.InvalidateMeasure();
            }
        }
    }

    internal void RefreshColumnHeaders(CovenTableViewColumn column) {
        int columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0) {
            return;
        }

        HeadersPresenter?.RefreshHeader(columnIndex);
    }

    internal void RefreshColumnCells(CovenTableViewColumn column) {
        int columnIndex = Columns.IndexOf(column);
        if (columnIndex < 0) {
            return;
        }

        foreach (Control row in GetRealizedContainers()) {
            if (row is CovenTableViewRow tableViewRow) {
                tableViewRow.RefreshCell(columnIndex);
            }
        }
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
        base.OnPropertyChanged(change);

        if (change.Property == CanUserResizeColumnsProperty && _columns is not null) {
            foreach (CovenTableViewColumn column in _columns) {
                column.UpdateCanUserEffectivelyResize();
            }
        }
    }
}
