using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using gex.Coven.ViewModels.Match;
using System.Collections;

namespace gex.Coven.Controls.MatchWindow {

    public partial class MatchViewChatMessages : UserControl {

        public static readonly StyledProperty<BarMatchChatMessagesViewModel> MessagesProperty =
            AvaloniaProperty.Register<MatchViewChatMessages, BarMatchChatMessagesViewModel>(nameof(Messages));

        public MatchViewChatMessages() {
            InitializeComponent();
        }

        public BarMatchChatMessagesViewModel Messages {
            get => GetValue(MessagesProperty);
            set => SetValue(MessagesProperty, value);
        }

    }
}