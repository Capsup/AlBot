using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Models.GamesModule.AdminModule
{
    public class ScheduledGame : ModelBase
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

        private DateTime _startTime;
        [BsonDateTimeOptions( Kind = DateTimeKind.Utc, Representation = MongoDB.Bson.BsonType.String )]
        public DateTime StartTime
        {
            get
            {
                return _startTime;
            }
            set
            {
                _startTime = value;
            }
        }

        private string _description;
        public string Description
        {
            get
            {
                return _description;
            }
            set
            {
                _description = value;
            }
        }

        private ulong[] _confirmedUserIds;
        public ulong[] ConfirmedUserIds
        {
            get
            {
                return _confirmedUserIds;
            }
            set
            {
                _confirmedUserIds = value;
            }
        }

        private string _jobId;
        public string JobId
        {
            get
            {
                return _jobId;
            }
            set
            {
                _jobId = value;
            }
        }
    }
}
