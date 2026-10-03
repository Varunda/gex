using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;

namespace gex.Coven.Table.Controls;

/// <summary>
/// Represents a single cell in a <see cref="TableViewRow"/>.
/// </summary>
public class CovenTableViewCell : ContentControl {

    private static CompiledBinding RowBinding => field ??= new();

    public static readonly DirectProperty<CovenTableViewCell, CovenTableViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<CovenTableViewCell, CovenTableViewColumn?>(nameof(Column), o => o.Column);

    /// <summary>
    /// Gets the column associated with this cell.
    /// </summary>
    public CovenTableViewColumn? Column {
        get;
        internal set {
            CovenTableViewColumn? oldValue = field;
            if (!SetAndRaise(ColumnProperty, ref field, value)) {
                return;
            }

            if (oldValue is not null) {
                ClearProperties();
            }

            if (value is not null) {
                SetProperties(value);
            }
        }
    }

    private void ClearProperties() {
        ClearValue(IsVisibleProperty);
        ClearValue(ThemeProperty);
        ClearValue(HorizontalContentAlignmentProperty);
        ClearValue(ContentTemplateProperty);
        ClearValue(ContentProperty);
    }

    private void SetProperties(CovenTableViewColumn column) {
        // We don't bind the various properties here.
        // First, it's pretty rare for a column's properties to change after the initial setup.
        // Second, we have additional logic depending on whether a cell template is specified.
        // Instead, values are updated manually via Refresh().

        SetValue(IsVisibleProperty, column.IsVisible);
        SetValue(ThemeProperty, column.CellTheme);
        SetValue(HorizontalContentAlignmentProperty, column.HorizontalContentAlignment);

        if (column.CellTemplate is { } cellTemplate) {
            SetValue(ContentTemplateProperty, cellTemplate);
            Bind(ContentProperty, RowBinding);
        } else {
            SetValue(ContentTemplateProperty, null);
            Bind(ContentProperty, column.Binding ?? RowBinding);
        }
    }

    internal void Refresh() {
        if (Column is not null) {
            SetProperties(Column);
        }
    }

}
