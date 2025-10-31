using gex.Common.Models.Familiar;
using System.Threading.Tasks;

namespace gex.Code.Hubs {

    public interface IFamiliarHub {

        Task Hello(string name);

        Task SendLua(FamiliarSendWidgetMessage msg);

        Task JoinGame(FamiliarJoinBattleMessage msg);

        Task LaunchGame(FamiliarLaunchGameMessage msg);

        Task LeaveLobby(FamiliarLeaveLobbyMessage msg);

    }
}
