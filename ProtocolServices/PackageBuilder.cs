using OmahaPokerServer.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static OmahaPokerServer.ProtocolServices.OmahaPackageHelper;
namespace OmahaPokerServer.ProtocolServices
{
    public class PackageBuilder
    {
        private readonly byte[] package;
        public PackageBuilder(int sizeOfContent)
        {
            if (sizeOfContent > MaxSizeOfContent)
            {
                throw new ArgumentException(
                $"size of content must be less or equal {nameof(MaxSizeOfContent)}",
                nameof(sizeOfContent));
            }

            package = new byte[MaxFreeBytes + sizeOfContent];
            CreateBasePackage();
        }

        private void CreateBasePackage()
        {
            Array.Copy(BasePackage, package, BasePackage.Length);
            package[^1] = LastSymbol;
        }

        public PackageBuilder WithCommand(Commands comand)
        {
            package[Command] = (byte)comand;
            return this;
        }

        public PackageBuilder WithQuery(QueryType queryType)
        {
            package[Query] = (byte)queryType;
            return this;
        }

        public PackageBuilder WithStatus(StatusName status)
        {
            package[Status] = (byte)status;
            return this;
        }

        public PackageBuilder WithContent(byte[] content)
        {
            if (content.Length > package.Length - MaxFreeBytes)
            {
                throw new ArgumentException(nameof(content));
            }

            for (var i = 0; i < content.Length; i++)
            {
                package[i + MaxFreeBytes - 1] = content[i];
            }
            return this;
        }

        public byte[] Build()
        {
            return package;
        }
    }
}
