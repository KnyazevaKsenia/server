using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using static OmahaPokerServer.ProtocolServices.OmahaPackageHelper;

namespace OmahaPokerServer
{
    public class PokerServerSession(IPAddress address, int port, int maxClientsCount) : PokerServer(address, port)
    {
        protected readonly Socket _socket;
        protected const int MaxTimeout = 5 * 60 * 1000;
        protected readonly Dictionary<Socket, (int, int)> _clients = new();
        public int MaxClientsCount { get; private set; } = maxClientsCount;
        public override async Task StartAsync()
        {
            try
            {
                _socket.Listen();
                do
                {
                    var cancellationToken = new CancellationTokenSource();
                    cancellationToken.CancelAfter(MaxTimeout);
                    cancellationToken.Token.Register(async () =>
                    {
                        if (_clients.Count == 0)
                        {
                            StopAsync();
                        }
                    });

                    var connectionSocket = await _socket.AcceptAsync(cancellationToken.Token);
                    _clients.Add(connectionSocket, (0, 0));
                    var innerCancellationToken = new CancellationTokenSource();
                    _ = Task.Run(
                        async () =>
                            await ProcessSessionConnection(connectionSocket, innerCancellationToken), innerCancellationToken.Token);
                }
                while (_clients.Count != 0 && _clients.Count != MaxClientsCount);
            }
            catch (TaskCanceledException tcex)
            {
                Console.WriteLine(tcex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                StopAsync();
            }
        }
        public async Task ProcessSessionConnection(Socket socket, CancellationTokenSource cancellationTokenSource)
        {
            try
            {
                socket.ReceiveTimeout = MaxTimeout;
                socket.SendTimeout = MaxTimeout;

                var buffer = new byte[MaxPacketSize];
                var recievedLength = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationTokenSource.Token);
                if (buffer.Length != recievedLength)
                {
                    buffer = buffer.Take(recievedLength).ToArray();
                }

                if (IsQueryValid(buffer, recievedLength) && IsConnect(buffer))
                {
                    var badRequestContent = Encoding.UTF8.GetBytes("You connected to the game");
                    var package = CreatePackage(badRequestContent, Enums.Commands.None, Enums.QueryType.Response, Enums.StatusName.None);
                    await SendResponseToClientAsync(socket, package, cancellationTokenSource.Token);
                    Console.WriteLine("New player connected to session");
                }
                else
                {
                    _clients.Remove(socket);
                    await socket.DisconnectAsync(false);
                    Console.WriteLine("Player with wrong protocol");
                }
                while (socket.Connected)
                {

                }

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке соединения: {ex.Message}");
            }
        }
    }
}
