using System;
using System.Collections.Generic;
using System.Text;

namespace hic
{
    public class Options
    {
        public string ServerName = null;
        public string UserName = null;
        public string Password = null;
        public bool PrintToConsole = false;
        public IList<string> Tags = null;
        public DateTime Start = DateTime.MinValue;
        public DateTime End = DateTime.MinValue;

        public bool Validate()
        {
            return ServerName != null && Tags != null && Start != DateTime.MinValue && End != DateTime.MinValue;
        }
    }
}
