namespace gex.Common.Code.Constants {

    public class BarPacketType {

        public const int KEYFRAME = 1;

        public const int NEW_FRAME = 2;

        public const int QUIT = 3;

        public const int START_PLAYING = 4;

        public const int CHAT = 7;

        public const int GAME_ID = 9;

        public const int COMMAND = 11;

        public const int SELECT = 12;

        public const int PAUSE = 13;

        public const int AI_COMMAND = 14;

        public const int AI_COMMANDS = 15;

        public const int GAME_OVER = 30;

        public const int MAP_DRAW_OLD = 31;

        /// <summary>
        ///     new map draw command as of 2025.04
        ///     https://github.com/beyond-all-reason/RecoilEngine/blob/55e41dff5c551684c69c8e79b957180965973493/doc/site/content/changelogs/changelog-2025-04.markdown?plain=1#L10
        /// </summary>
        public const int MAP_DRAW = 32;

        public const int START_POS = 36;

        public const int LUA_MSG = 50;

        // uint8_t playerNum, uint8_t action, uint8_t parameter1
        public const int TEAM_MSG = 51;

    }
}
