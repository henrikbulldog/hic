using Proficy.Historian.ClientAccess.API;
using System;

namespace hic
{
    public class Options
    {
        public string ServerName = null;
        public string UserName = null;
        public string Password = null;
        public string Out = null;
        public DataCriteria Criteria;

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
