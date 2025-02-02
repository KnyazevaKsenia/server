using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmahaPokerServer.Enums
{
    public enum Commands : byte
    {
        Connect = 0x2B, //+
        Regist = 0x52, //R
        Login = 0x4C, //L
        CreateSession = 0x43,//C
        JoinTheGame = 0x4A, //J
        None = 0x00, //null
    }
}
