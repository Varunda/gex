using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Coven.Models.Ui;
using System.Windows.Input;

namespace gex.Coven.Controls {

    public class TableHeaderSorter : TemplatedControl {

        #region properties

        public static readonly StyledProperty<string> HeaderNameProperty = AvaloniaProperty.Register<TableHeaderSorter, string>(
            name: nameof(HeaderName),
            defaultValue: ""
        );

        public string HeaderName {
            get => GetValue(HeaderNameProperty);
            set => SetValue(HeaderNameProperty, value);
        }

        public static readonly StyledProperty<string?> HeaderDescriptionProperty = AvaloniaProperty.Register<TableHeaderSorter, string?>(
            name: nameof(HeaderDescription),
            defaultValue: null
        );

        public string? HeaderDescription {
            get => GetValue(HeaderDescriptionProperty);
            set => SetValue(HeaderDescriptionProperty, value);
        }

        public static readonly StyledProperty<string> FieldNameProperty = AvaloniaProperty.Register<TableHeaderSorter, string>(
            name: nameof(FieldName),
            defaultValue: ""
        );

        public string FieldName {
            get => GetValue(FieldNameProperty);
            set => SetValue(FieldNameProperty, value);
        }

        public static readonly StyledProperty<string> SortFieldValueProperty = AvaloniaProperty.Register<TableHeaderSorter, string>(
            name: nameof(SortFieldValue),
            defaultValue: ""
        );

        public string SortFieldValue {
            get => GetValue(SortFieldValueProperty);
            set => SetValue(SortFieldValueProperty, value);
        }

        public static readonly StyledProperty<ICommand?> SortCommandProperty = AvaloniaProperty.Register<TableHeaderSorter, ICommand?>(
            name: nameof(SortCommand),
            defaultValue: null
        );

        public ICommand? SortCommand {
            get => GetValue(SortCommandProperty);
            set => SetValue(SortCommandProperty, value);
        }

        public static readonly StyledProperty<SortDirection> SortDirectionProperty = AvaloniaProperty.Register<TableHeaderSorter, SortDirection>(
            name: nameof(SortDirection),
            defaultValue: SortDirection.Desc
        );

        public SortDirection SortDirection {
            get => GetValue(SortDirectionProperty);
            set => SetValue(SortDirectionProperty, value);
        }

        public static readonly DirectProperty<TableHeaderSorter, bool> DescVisibleProperty =
            AvaloniaProperty.RegisterDirect<TableHeaderSorter, bool>(
                nameof(DescVisible),
                o => o.DescVisible
            );

        private bool _DescVisible = true;
        public bool DescVisible {
            get => _DescVisible;
            private set => SetAndRaise(DescVisibleProperty, ref _DescVisible, value);
        }

        public static readonly DirectProperty<TableHeaderSorter, bool> AscVisibleProperty =
            AvaloniaProperty.RegisterDirect<TableHeaderSorter, bool>(
                nameof(AscVisible),
                o => o.AscVisible
            );

        private bool _AscVisible = true;
        public bool AscVisible {
            get => _AscVisible;
            private set => SetAndRaise(AscVisibleProperty, ref _AscVisible, value);
        }

        #endregion

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change) {
            base.OnPropertyChanged(change);

            if (change.Property == SortDirectionProperty || change.Property == SortFieldValueProperty) {
                if (SortFieldValue == FieldName) {
                    DescVisible = SortDirection == SortDirection.Desc;
                    AscVisible = SortDirection == SortDirection.Asc;
                } else {
                    DescVisible = false;
                    AscVisible = false;
                }
            }
        }

    }
}