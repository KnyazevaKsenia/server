using System.Net;
using System.Net.Sockets;
using System.Text;
using static OmahaPokerServer.ProtocolServices.OmahaPackageHelper;

namespace OmahaPokerServer
{
    public class PokerServer
    {
        private readonly Socket _socket;
        private const int MaxTimeout = 5 * 60 * 1000;
        private readonly Dictionary<Socket, int> _clients = new();
        private readonly PokerDbContext _dbContext = new PokerDbContext();
        private readonly List<PokerSession> sessions = new List<PokerSession>();

        public PokerServer(IPAddress address, int port)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.Bind(new IPEndPoint(address, port));
        }

        public async Task StartAsync()
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
                    _clients.Add(connectionSocket, 0);
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
        private void StopAsync()
        {
            _socket.Close();
        }

        private async Task ProcessSocketConnection(Socket socket, CancellationTokenSource cancellationToken)
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
                            if (result == 0)
                            {
                                var badRequestContent = Encoding.UTF8.GetBytes("Wrong data");
                                var package = CreatePackage(badRequestContent, Enums.Commands.None, Enums.QueryType.Response, Enums.StatusName.Failed);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                            else
                            {
                                _clients[socket] = result;
                                var successRequestContent = Encoding.UTF8.GetBytes($"You have registered, your id:{result}");
                                var package = CreatePackage(successRequestContent, Enums.Commands.None, Enums.QueryType.Response, Enums.StatusName.Success);
                                await SendResponseToClientAsync(socket, package, cancellationToken.Token);
                            }
                        }
                      
                        if (IsLogin(processing_buffer))
                        {
                            
                        }
                        if (IsCreateSession(processing_buffer))
                        {
                            // Обработка создания сессии
                        }
                        if (IsJoinTheGame(processing_buffer))
                        {
                            // Обработка присоединения к игре
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

        public async Task<int> RegistUser(byte[] buffer, int contentLength, CancellationTokenSource token)
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
                    return savingResult;
                }
                else
                {
                    return 0;
                }
            }
            else
            {
                return 0;
            }
        }

        private async Task SendResponseToClientAsync(Socket clientSocket, byte[] buffer, CancellationToken cancellationToken)
        {
            try
            {
                await clientSocket.SendAsync(buffer, SocketFlags.None, cancellationToken);
                Console.WriteLine("Сообщение отправлено клиенту.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения клиенту: {ex.Message}");
            }
        }
    }
}
