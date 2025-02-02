using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OmahaPokerServer
{
    public class PokerSession
    {
        public string Name { get; set; }
        int AmountOfPlayers {  get; set; }
        List<int> PlayersId {  get; set; }
        int Bank {  get; set; }
        public PokerSession(string name,int amountOfPlayers, int bank) 
        {
            Name = name;
            this.AmountOfPlayers = amountOfPlayers;
            Bank= bank;
        }
        public void AddPlayer(int playerId)
        {
            PlayersId.Add(playerId);
        }
    }
}
