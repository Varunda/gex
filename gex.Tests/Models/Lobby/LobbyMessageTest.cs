using gex.Common.Models.Lobby;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gex.Tests.Models.Lobby {

    [TestClass]
    public class LobbyMessageTest {

        [TestMethod]
        public void Test_GetWord() {
            LobbyMessage msg = new();
            msg.Arguments = "Host[EU7][014] NL 123456 LuaLobby Chobby";

            string? username = msg.GetWord();
            Assert.AreEqual("Host[EU7][014]", username);

            string? country = msg.GetWord();
            Assert.AreEqual("NL", country);

            string? userID = msg.GetWord();
            Assert.AreEqual("123456", userID);

            string? lobbyID = msg.GetSentence();
            Assert.AreEqual("LuaLobby Chobby", lobbyID);

            string? nil = msg.GetWord();
            Assert.IsNull(nil);
        }

        [TestMethod]
        public void Test_GetSentence() {
            LobbyMessage msg = new();
            msg.Arguments = "hello there\thi there\thowdy hey!\n";

            Assert.AreEqual("hello there", msg.GetSentence());
            Assert.AreEqual("hi there", msg.GetSentence());
            Assert.AreEqual("howdy hey!", msg.GetSentence());
            Assert.IsNull(msg.GetSentence());
            Assert.IsNull(msg.GetWord());
        }

    }
}
