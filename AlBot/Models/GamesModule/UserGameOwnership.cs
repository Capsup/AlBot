using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Models.GamesModule
{
    public class UserGameOwnership : ModelBase
    {
        private ulong _userId;
        public ulong UserId
        {
            get
            {
                return _userId;
            }
            set
            {
                _userId = value;
            }
        }

        private string _gameId;
        public string GameId
        {
            get
            {
                return _gameId;
            }
            set
            {
                _gameId = value;
            }
        }

        private bool _isInterested;
        public bool IsInterested
        {
            get
            {
                return _isInterested;
            }
            set
            {
                _isInterested = value;
            }
        }

        private BitArray _interestedDays;
        public BitArray InterestedDays
        {
            get
            {
                return _interestedDays;
            }
            set
            {
                _interestedDays = value;
            }
        }

    }
}
