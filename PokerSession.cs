using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace OmahaPokerServer
{
    public class PokerSession
    {
        public IPAddress Address { get; set; }
        public int port { get; set; }
        public string Name { get; set; }
        public int NumberOfPlayers {  get; set; }
        List<int> PlayersId {  get; set; }
        public int Bank {  get; set; }
        public PokerSession(string name,int amountOfPlayers, int bank, IPAddress adress, int port) 
        {
            this.Name = name;
            this.NumberOfPlayers = amountOfPlayers;
            this.Bank= bank;
            this.Address = adress;
            this.port = port;
        }
        public void AddPlayer(int playerId)
        {
            PlayersId.Add(playerId);
        }
    }
}
