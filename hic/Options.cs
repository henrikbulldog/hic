using Proficy.Historian.ClientAccess.API;
using System;

namespace hic
{
    public class Options
    {
        private string _serverName;
        private string _username;
        private string _password;

        public string ServerName
        {
            get
            {
                return _serverName;
            }
            set
            {
                var env = Environment.GetEnvironmentVariable(value);
                if (env != null)
                {
                    _serverName = env;
                }
                else
                {
                    _serverName = value;
                }
            }
        }

        public string UserName
        {
            get
            {
                return _username;
            }
            set
            {
                var env = Environment.GetEnvironmentVariable(value);
                if (env != null)
                {
                    _username = env;
                }
                else
                {
                    _username = value;
                }
            }
        }

        public string Password
        {
            get
            {
                return _password;
            }
            set
            {
                var env = Environment.GetEnvironmentVariable(value);
                if (env != null)
                {
                    _password = env;
                }
                else
                {
                    _password = value;
                }
            }
        }

        public string Out = null;

        public DataCriteria Criteria;

        public long MaxMessageSize;

        public Options(object[] tagnames)
        {
            Criteria = new DataCriteria(tagnames);
            Criteria.IntervalMicroseconds = 60000000;
            Criteria.Start = DateTime.MinValue;
            Criteria.End = DateTime.MinValue;
        }

        public bool Validate()
        {
            return ServerName != null
                && UserName != null
                && Password != null
                && Criteria.Tagnames != null
                && Criteria.Start != DateTime.MinValue
                && Criteria.End != DateTime.MinValue
                && Criteria.SamplingMode != DataCriteria.SamplingModeType.Undefined
                && Criteria.CalculationMode != DataCriteria.CalculationModeType.Undefined
                && Criteria.IntervalMicroseconds != 0;
            ;
        }
    }
}
