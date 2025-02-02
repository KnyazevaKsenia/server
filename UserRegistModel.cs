using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmahaPokerServer
{
    public class UserRegistModel
    {
        public string Nickname {  get;private set; }
        public string Password {  get;private set; }
        public UserRegistModel(string nickname, string password) 
        {
            Nickname = nickname;
            Password = password;
        }
    }
}
