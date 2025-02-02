using OmahaPokerServer.Enums;
using System.Linq;

namespace OmahaPokerServer.ProtocolServices
{
    public static class OmahaPackageHelper
    {
        public const int MaxPacketSize = 256;
        public const int MaxSizeOfContent = 245;
        public const int MaxFreeBytes = MaxPacketSize - MaxSizeOfContent;

        public const int Command = 7;
        public const int Query = 8;
        public const int Status = 9;

        public static readonly byte[] BasePackage =
        {
        0x02, 0x02, 0x02,
        0x13, 0x02, 0x1A,
        0x06,
        }; 

        public const int LastSymbol = 0x03; //#

        public static byte[] GetContent(byte[] buffer, int contentLength) =>
            buffer.Skip(MaxFreeBytes - 1).Take(contentLength - MaxFreeBytes).ToArray();

        public static bool IsQueryValid(byte[] buffer, int packageLength) =>
            HasStart(buffer) && HasEnd(buffer) && IsCorrectProtocol(buffer)
            && HasQueryType(buffer);

        public static bool HasStart(byte[] buffer) => buffer.Take(3).SequenceEqual(BasePackage.Take(3));

        public static bool HasEnd(byte[] buffer) => buffer[^1] == LastSymbol;

        public static bool IsCorrectProtocol(byte[] buffer) => buffer.Skip(3).Take(4).SequenceEqual(BasePackage.Skip(3).Take(4));

        public static bool HasQueryType(byte[] buffer) =>
            buffer[Query] is (byte)QueryType.Request or (byte)QueryType.Response;

        public static bool IsRequest(byte[] buffer) =>
            buffer[Query] is (byte)QueryType.Request;

        public static bool IsResponse(byte[] buffer) =>
            buffer[Query] is (byte)QueryType.Response;

        public static bool IsConnect(byte[] buffer) =>
            buffer[Command] is (byte)Commands.Connect;
        public static bool IsRegist(byte[] buffer) =>
            buffer[Command] is (byte)Commands.Regist;

        public static bool IsLogin(byte[] buffer) =>
            buffer[Command] is (byte)Commands.Login;

        public static bool IsCreateSession(byte[] buffer) =>
            buffer[Command] is (byte)Commands.CreateSession;

        public static bool IsJoinTheGame(byte[] buffer) =>
            buffer[Command] is (byte)Commands.JoinTheGame;

        public static bool IsSuccess(byte[] buffer) =>
            buffer[Status] is (byte)StatusName.Success;

        public static bool IsFailed(byte[] buffer) =>
            buffer[Status] is (byte)StatusName.Failed;

        public static byte[] CreatePackage(byte[] content, Commands command, QueryType query, StatusName status) =>
            new PackageBuilder(content.Length).WithCommand(command).WithQuery(query).WithStatus(status).WithContent(content).Build();

        public static byte[] CreatePackageToConnect()=>
            new PackageBuilder(0).WithCommand(Commands.Connect).WithQuery(QueryType.Request).Build();
    }

}
