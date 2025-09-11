
# wireshark dump

a small tool to take a JSON dump of a Wireshark capture and print out the Spring lobby messages from it

spring lobby protocol description: https://springrts.com/dl/LobbyProtocol/ProtocolDescription.html

wireshark filters used: `(ip.addr == {SPRING IP} && (tcp.len > 0 or udp))`

where `{SPRING IP}` can be found by pinging the lobby host

## usage

`dotnet run .\Program.cs <input file> <output file>`