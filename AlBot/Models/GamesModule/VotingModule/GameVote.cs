using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Models.VotingModule
{
    public class GameVote : ModelBase
    {
        private string _gameName;
        public string GameName
        {
            get
            {
                return _gameName;
            }
            set
            {
                _gameName = value;
            }
        }

        private ulong _userLong;
        public ulong UserLong
        {
            get
            {
                return _userLong;
            }
            set
            {
                _userLong = value;
            }
        }
    }
}
