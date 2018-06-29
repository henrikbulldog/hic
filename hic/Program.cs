using Microsoft.Extensions.Configuration;
using Proficy.Historian.ClientAccess.API;
using System;
using System.Collections.Generic;
using System.Linq;

namespace hic
{
    class Program
    {

        static void Main(string[] args)
        {
            var options = new Options();
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();
            Console.WriteLine(string.Join(", ", config.AsEnumerable().Select(e => $"{e.Key}: {e.Value}")));
            options.ServerName = GetArg(config.AsEnumerable(), "server");
            options.UserName = GetArg(config.AsEnumerable(), "user");
            options.Password = GetArg(config.AsEnumerable(), "psw");
            options.Tags = Str2List(GetArg(config.AsEnumerable(), "tags"));
            DateTime.TryParse(GetArg(config.AsEnumerable(), "start"), out options.Start);
            DateTime.TryParse(GetArg(config.AsEnumerable(), "end"), out options.End);
            options.PrintToConsole = true;
            if (!options.Validate())
            {
                Help();
            }
            else
            {
                var ds = Query(options);
                PrintDataSet(ds);
            }
        }

        private static void PrintDataSet(DataSet ds)
        {
            if (ds != null)
            {
                Console.WriteLine($"TotalSamples: { ds.TotalSamples }, first 10:");
                foreach (var r in ds.Take(10))
                {
                    Console.WriteLine($"{r.Key}: {string.Join(", ", r.Value.Values().Cast<object>().ToList())}");
                }
            }
        }

        private static IList<string> Str2List(string str)
        {
            if (str != null)
                return str.Split(',').ToList();
            return null;
        }

        private static void Help()
        {
            Console.WriteLine("Usage: hic --server <server dns or ip> --user <user name> --psw <password> --tags <tag names> --start <start time> --end <end time>");
        }

        private static string GetArg(IEnumerable<KeyValuePair<string, string>> args, string key)
        {
            return args.FirstOrDefault(a => a.Key == key).Value;
        }

        public static DataSet Query(Options options)
        {
            ServerConnection _historian;
            try
            {
                // Define connection and establish it
                _historian = new ServerConnection(new ConnectionProperties { ServerHostName = options.ServerName, Username = options.UserName, Password = options.Password, ServerCertificateValidationMode = CertificateValidationMode.None });
                _historian.Connect();
                var parms = new DataQueryParams(options.Tags);
                parms.Criteria.SamplingMode = DataCriteria.SamplingModeType.Calculated;
                parms.Criteria.CalculationMode = DataCriteria.CalculationModeType.Average;
                parms.Criteria.BackwardTimeOrder = false;
                parms.Criteria.IntervalMicroseconds = 3600000;
                parms.Criteria.NumberOfSamples = 0;
                parms.Criteria.Start = options.Start;
                parms.Criteria.End = options.End;
                DataSet ds;
                ItemErrors errors;

                _historian.IData.Query(ref parms, out ds, out errors);

                if (errors.Count() > 0)
                {
                    throw new Exception(string.Join(", ", errors.Select(e => $"{e.Key}: {e.Value}")));
                }
                return ds;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Proficy.Historian.Module.QueryModule - Error while querying: " + ex.Message);
            }
            return null;
        }

    }
}
