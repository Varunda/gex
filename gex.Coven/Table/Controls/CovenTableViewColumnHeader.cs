using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Metadata;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using System;
using System.Text;

namespace gex.Coven.Table.Controls;

/// <summary>
/// Represents the header of a <see cref="TableViewColumn"/>.
/// </summary>
[TemplatePart(PartResizer, typeof(Thumb))]
public class CovenTableViewColumnHeader : ContentControl {
    private const string PartResizer = "PART_Resizer";

    /// <summary>
    /// Identifies the <see cref="Column"/> property.
    /// </summary>
    public static readonly DirectProperty<CovenTableViewColumnHeader, CovenTableViewColumn?> ColumnProperty =
        AvaloniaProperty.RegisterDirect<CovenTableViewColumnHeader, CovenTableViewColumn?>(nameof(Column), o => o.Column);

    private Thumb? _resizer;

    /// <summary>
    /// Gets the column associated with this header.
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

    /// <inheritdoc />
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e) {
        base.OnApplyTemplate(e);

        _resizer?.DragDelta -= OnResizerDragDelta;
        _resizer = e.NameScope.Find<Thumb>(PartResizer);
        _resizer?.DragDelta += OnResizerDragDelta;
    }

    private void OnResizerDragDelta(object? sender, VectorEventArgs e) {
        if (Column is not { CanUserEffectivelyResize: true } column) {
            return;
        }

        double actualWidth = column.ActualWidth;
        if (double.IsNaN(actualWidth)) {
            return;
        }

        double minWidth = _resizer?.Bounds.Width ?? 0;
        double newWidth = Math.Max(minWidth, actualWidth + e.Vector.X);
        column.Width = new GridLength(newWidth, GridUnitType.Pixel);
    }

    /*
    internal override void BuildDebugDisplay(StringBuilder builder, bool includeContent) {
        base.BuildDebugDisplay(builder, includeContent);

        if (includeContent) {
            DebugDisplayHelper.AppendOptionalValue(builder, nameof(Column), Column?.Header, includeContent);
        }
    }
    */

    private void ClearProperties() {
        ClearValue(IsVisibleProperty);
        ClearValue(ThemeProperty);
        ClearValue(HorizontalContentAlignmentProperty);
        ClearValue(ContentTemplateProperty);
        ClearValue(ContentProperty);
    }

    private void SetProperties(CovenTableViewColumn column) {
        SetValue(IsVisibleProperty, column.IsVisible);
        SetOrClearValue(ThemeProperty, column.HeaderTheme);
        SetValue(HorizontalContentAlignmentProperty, column.HorizontalContentAlignment);
        SetOrClearValue(ContentTemplateProperty, column.HeaderTemplate);
        SetOrClearValue(ContentProperty, column.Header);

        void SetOrClearValue<T>(StyledProperty<T?> property, T? value) where T : class {
            if (value is not null) {
                SetValue(property, value);
            } else {
                ClearValue(property);
            }
        }
    }

    internal void Refresh() {
        if (Column is not null) {
            SetProperties(Column);
        }
    }

}
