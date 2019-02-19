using Proficy.Historian.ClientAccess.API;
using System;

namespace hic
{
    public class Options
    {
        private string _serverName;
        private string _username;
        private string _password;

				public string TagMask { get; }

				public long IntervalMicroseconds;

				public uint NumberOfSamples;

				public DateTime Start;

				public DateTime End;

				public DataCriteria.SamplingModeType SamplingMode;

				public DataCriteria.CalculationModeType CalculationMode;

				public long MaxMessageSize;

				public string Out = null;

				public string ServerName
        {
            get
            {
                return _serverName;
            }
            set
            {
                var env = Environment.GetEnvironmentVariable(value ?? "");
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
                var env = Environment.GetEnvironmentVariable(value ?? "");
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
                var env = Environment.GetEnvironmentVariable(value ?? "");
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

        public Options(string tagMask)
        {
						TagMask = tagMask;
            IntervalMicroseconds = 0;
						NumberOfSamples = 1;
        }

				public bool Validate()
        {
            return ServerName != null
                && UserName != null
                && Password != null
                && TagMask != null
                && (SamplingMode == DataCriteria.SamplingModeType.CurrentValue ||
                    (SamplingMode != DataCriteria.SamplingModeType.Undefined
                    && CalculationMode != DataCriteria.CalculationModeType.Undefined)
                   );
        }
    }
}
