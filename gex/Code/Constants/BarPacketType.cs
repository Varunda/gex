namespace gex.Code.Constants {

    public class BarPacketType {

        public const int KEYFRAME = 1;

        public const int NEW_FRAME = 2;

        public const int QUIT = 3;

        public const int CHAT = 7;

        public const int GAME_ID = 9;

        public const int GAME_OVER = 30;

        public const int START_POS = 36;

        public const int LUA_MSG = 50;

        // uint8_t playerNum, uint8_t action, uint8_t parameter1
        public const int TEAM_MSG = 51;

    }
}
