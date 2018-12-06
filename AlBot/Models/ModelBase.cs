using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Models
{
    public abstract class ModelBase
    {
        [MongoDB.Bson.Serialization.Attributes.BsonId]
        [BsonIgnoreIfDefault]
        public string Id
        {
            get;
            set;
        }

        public string CreatedBy
        {
            get;
            set;
        }

        [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions( Kind = DateTimeKind.Utc )]
        public DateTime? ModifiedDateTime
        {
            get;
            set;
        }

        public string ModifiedBy
        {
            get;
            set;
        }

        private bool? _deleted;
        public bool? Deleted
        {
            get
            {
                return _deleted ?? false;
            }
            set
            {
                _deleted = value;
            }
        }

        public string DeletedBy
        {
            get;
            set;
        }

        [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions( Kind = DateTimeKind.Utc )]
        public DateTime? DeletedDateTime
        {
            get;
            set;
        }

        private DateTime _createDateTime;
        [MongoDB.Bson.Serialization.Attributes.BsonDateTimeOptions( Kind = DateTimeKind.Utc )]
        [BsonIgnoreIfDefault]
        public DateTime CreatedDateTime
        {
            get
            {
                return _createDateTime = ( ( _createDateTime == null || _createDateTime == default( DateTime ) ) && !string.IsNullOrEmpty( this.Id ) ? MongoDB.Bson.ObjectId.Parse( this.Id ).CreationTime : _createDateTime );
            }
            set
            {
                _createDateTime = value.Kind == DateTimeKind.Unspecified || value.Kind == DateTimeKind.Local ? value.ToUniversalTime() : value;
            }
        }

        public ModelBase ShallowCopy()
        {
            return (ModelBase) this.MemberwiseClone();
        }
    }
}
