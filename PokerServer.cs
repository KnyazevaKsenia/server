using System.Linq.Expressions;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using static OmahaPokerServer.ProtocolServices.OmahaPackageHelper;

namespace OmahaPokerServer
{
    public class PokerServer
    {
        protected readonly Socket _socket;
        protected const int MaxTimeout = 5 * 60 * 1000;
        protected readonly Dictionary<Socket, (int,int)> _clients = new();
        protected readonly PokerDbContext _dbContext = new PokerDbContext();
        protected readonly List<PokerSession> sessions = new List<PokerSession>();
        int currentPort {  get; set; }
        public PokerServer(IPAddress address, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(address, port));
            currentPort = port;
        }

        public virtual async Task StartAsync()
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
                            await ProcessSocketConnection(connectionSocket, innerCancellationToken), innerCancellationToken.Token);
                }
                while (_clients.Count != 0);
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
        protected void StopAsync()
        {
            _socket.Close();
        }

        protected async Task ProcessSocketConnection(Socket socket, CancellationTokenSource cancellationToken)
        {
            try
            {
                socket.ReceiveTimeout = MaxTimeout;
                socket.SendTimeout = MaxTimeout;

                var buffer = new byte[MaxPacketSize];
                var recievedLength = await socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken.Token);
                if (buffer.Length != recievedLength)
                {
                    buffer = buffer.Take(recievedLength).ToArray();
                }

                if (IsQueryValid(buffer, recievedLength) && IsConnect(buffer))
                {
                    var badRequestContent = Encoding.UTF8.GetBytes("Welcome to Omaha Poker");
                    var package = CreatePackage(badRequestContent, Enums.Commands.None, Enums.QueryType.Response, Enums.StatusName.None);
                    await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                    Console.WriteLine("New user connected");
                }
                else
                {
                    _clients.Remove(socket);
                    await socket.DisconnectAsync(false);
                    Console.WriteLine("User with wrong protocol");
                }
                
                while (socket.Connected)
                {
                    byte[] processing_buffer = new byte[256];
                    var recievedProcessingLength = await socket.ReceiveAsync(processing_buffer, SocketFlags.None, cancellationToken.Token);
                    if (processing_buffer.Length != recievedProcessingLength)
                    {
                        processing_buffer = processing_buffer.Take(recievedProcessingLength).ToArray();
                    }
                    if (IsQueryValid(processing_buffer, recievedLength) && IsRequest(processing_buffer))
                    {
                        if (IsRegist(processing_buffer))
                        {
                            var result = await RegistUser(processing_buffer, recievedProcessingLength, cancellationToken);
                            if (result == (0, 0))
                            {
                                var badRequestContent = Encoding.UTF8.GetBytes("Wrong data");
                                var package = CreatePackage(badRequestContent, Enums.Commands.Regist, Enums.QueryType.Response, Enums.StatusName.Failed);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                            else
                            {
                                _clients[socket] = result;
                                var successRequestContent = Encoding.UTF8.GetBytes($"You have registered, your id:{result.Item1}, your wallet:{result.Item2}");
                                var package = CreatePackage(successRequestContent, Enums.Commands.Regist, Enums.QueryType.Response, Enums.StatusName.Success);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                        }
                        if (IsLogin(processing_buffer))
                        {
                            (int, int) result = await LoginUser(processing_buffer, recievedLength, cancellationToken);
                            if (result == (0, 0))
                            {
                                var badRequestContent = Encoding.UTF8.GetBytes("Wrong data");
                                var package = CreatePackage(badRequestContent, Enums.Commands.None, Enums.QueryType.Response, Enums.StatusName.Failed);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                            else
                            {
                                _clients[socket] = result;
                                var successRequestContent = Encoding.UTF8.GetBytes($"You have logged in, your id:{result.Item1}, your wallet:{result.Item2}");
                                var package = CreatePackage(successRequestContent, Enums.Commands.Login, Enums.QueryType.Response, Enums.StatusName.Success);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                        }
                        if (IsCreateSession(processing_buffer))
                        {
                            var session = CreateSession(processing_buffer, recievedLength, cancellationToken.Token);
                            if (session != null) 
                            {
                                sessions.Add(session);
                                var sessionServer = new PokerServerSession(session.Address, session.port, session.NumberOfPlayers);
                                //запуск сервера для игры 
                                sessionServer.StartAsync();
                                var successRequestContent = Encoding.UTF8.GetBytes($"You have created session with name: {session.Name}, with bank: {session.Bank}, with {session.NumberOfPlayers} players");
                                var package = CreatePackage(successRequestContent, Enums.Commands.CreateSession, Enums.QueryType.Response, Enums.StatusName.Success);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                            else
                            {
                                var successRequestContent = Encoding.UTF8.GetBytes($"Wrong data for creating session");
                                var package = CreatePackage(successRequestContent, Enums.Commands.CreateSession, Enums.QueryType.Response, Enums.StatusName.Failed);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                        }
                        if (IsGetSessions(processing_buffer))
                        {

                        }
                        if (IsJoinTheGame(processing_buffer))
                        {
                            
                        }
                    }
                    else
                    {
                        Console.WriteLine("Попытался подключиться клиент без нужного протокола");
                        await socket.DisconnectAsync(false);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке соединения: {ex.Message}");
            }
        }
        private PokerSession CreateSession(byte[] buffer, int length, CancellationToken cancellationToken)
        {
            var contentString = Encoding.UTF8.GetString(GetContent(buffer, length));
            string[] parts = contentString.Split('&');
            string name = parts[0];
            try
            {
                int playersAmount = int.Parse(parts[1]);
                int bank = int.Parse(parts[2]);
                currentPort += 1;
                if (string.IsNullOrEmpty(name) && playersAmount != 0 && bank != 0)
                {
                    var session = new PokerSession(name, playersAmount, bank, new IPAddress(new byte[] { 127, 0, 0, 1 }), currentPort);
                    return session;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return null;
            }
        }

        private async Task<(int, int)> LoginUser(byte[] buffer, int contentLength, CancellationTokenSource token)
        {
            var contentString = Encoding.UTF8.GetString(GetContent(buffer, contentLength));
            string[] parts = contentString.Split('&');
            if (parts.Length == 2)
            {
                string nickname = parts[0];
                string password = parts[1];
                var user = new UserRegistModel(nickname, password);
                var result = await _dbContext.SavePlayer(nickname, password, token.Token);
                if (result != (0, 0))
                {
                    return (result.playerId, result.wallet);
                }
                else
                {
                    return (0,0);
                }
            }
            else
            {
                return (0, 0);
            }
        }

        private async Task<(int, int)> RegistUser(byte[] buffer, int contentLength, CancellationTokenSource token)
        {
            var contentString = Encoding.UTF8.GetString(GetContent(buffer, contentLength));
            string[] parts = contentString.Split('&');
            if (parts.Length == 2)
            {
                string nickname = parts[0];
                string password = parts[1];
                var user = new UserRegistModel(nickname, password);
                var userValidatorRule = new UserValidationRules();
                var userValidationResult = userValidatorRule.Validate(user);
                if (userValidationResult.IsValid)
                {
                    var savingResult = await _dbContext.SavePlayer(nickname, password, token.Token);
                    return (savingResult.playerId, savingResult.wallet);
                }
                else
                {
                    return (0,0);
                }
            }
            else
            {
                return (0,0);
            }
        }

        protected async Task SendResponseToClientAsync(Socket clientSocket, byte[] buffer, CancellationToken cancellationToken)
        {
            try
            {
                await clientSocket.SendAsync(buffer, SocketFlags.None, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения клиенту: {ex.Message}");
            }
        }
    }
}
