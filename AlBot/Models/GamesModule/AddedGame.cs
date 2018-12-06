using System;
using System.Collections.Generic;
using System.Text;

namespace AlBot.Models.GamesModule
{
    public class AddedGame : ModelBase
    {
        private string _name;
        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
    }
}
