using System.Net;
using OmahaPokerServer;

var server = new PokerServer(new IPAddress(new byte[] { 127, 0, 0, 1 }), 5001);
Console.WriteLine("Server started");
await server.StartAsync();

Console.WriteLine("Server was stoped!");