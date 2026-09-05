using Avalonia;
using Avalonia.Controls.Primitives;
using CommunityToolkit.Mvvm.ComponentModel;
using gex.Common.Models.Match;
using System;
using System.Collections.Generic;

namespace gex.Coven.Controls.MatchWindow {

    public class MatchWindowChat : TemplatedControl {

        public static readonly DirectProperty<MatchWindowChat, IEnumerable<BarMatchChatMessage>> MessagesProperty
            = AvaloniaProperty.RegisterDirect<MatchWindowChat, IEnumerable<BarMatchChatMessage>>(
                nameof(Messages),
                getter: o => o.Messages
        );

        private List<BarMatchChatMessage> _Messages = [];
        public IEnumerable<BarMatchChatMessage> Messages {
            get => _Messages;
            private set => SetValue(MessagesProperty, value);
        }

    }
}