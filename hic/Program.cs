using Microsoft.Extensions.Configuration;
using Proficy.Historian.ClientAccess.API;
using System;
using System.Collections.Generic;
using System.IO;
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
            options.Tags = Str2Array(GetArg(config.AsEnumerable(), "tags"));
            DateTime.TryParse(GetArg(config.AsEnumerable(), "start"), out options.Start);
            DateTime.TryParse(GetArg(config.AsEnumerable(), "end"), out options.End);
            options.Out = GetArg(config.AsEnumerable(), "out");

            options.PrintToConsole = true;
            if (!options.Validate())
            {
                Help();
            }
            else
            {
                var ds = Query(options);
                PrintDataSet(ds);
                SaveDataSetToFile(ds, options.Out);
            }
        }

        private static void SaveDataSetToFile(DataSet ds, string fileName)
        {
            if (fileName != null && ds != null)
            {
                Console.WriteLine($"Writing to file: { fileName }");
                var csvfile = File.CreateText(fileName);
                csvfile.WriteLine("Tag;TimeStamp;Quality;Value");
                foreach (var r in ds)
                {
                    for (int i = 0; i < r.Value.Count(); i++)
                    {
                        csvfile.WriteLine($"{r.Key};{r.Value.GetTime(i).ToUniversalTime().ToString("o")};{r.Value.GetQuality(i)};{r.Value.GetValue(i)}");
                    }
                }
                csvfile.Flush();
                csvfile.Close();
            }
        }

        private static void PrintDataSet(DataSet ds)
        {
            if (ds != null)
            {
                Console.WriteLine($"TotalSamples: { ds.TotalSamples }");
                foreach (var r in ds)
                {
                    Console.WriteLine($"Tag: {r.Key}");
                    for (int i = 0; i < Math.Min(r.Value.Count(), 3); i++)
                    {
                        Console.WriteLine($"Timestamp: {r.Value.GetTime(i).ToUniversalTime().ToString("o")} Quality: {r.Value.GetQuality(i)} Value: {r.Value.GetValue(i)}");
                    }
                }
            }
        }

        private static string[] Str2Array(string str)
        {
            if (str != null)
                return str.Split(',');
            return null;
        }

        private static void Help()
        {
            Console.WriteLine("Usage: hic --server <server dns or ip> --user <user name> --psw <password> --tags <tag names> --start <start time> --out <end time> [--out <output csv file>]");
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
                Console.WriteLine($"TagNames: {string.Join(", ", parms.Criteria.Tagnames)}");
                parms.Criteria.SamplingMode = DataCriteria.SamplingModeType.Calculated;
                parms.Criteria.CalculationMode = DataCriteria.CalculationModeType.Average;
                parms.Criteria.BackwardTimeOrder = false;
                parms.Criteria.IntervalMicroseconds = 60000000;
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
                Console.WriteLine($"Proficy.Historian.Module.QueryModule - Error while querying: {ex}");
            }
            return null;
        }

    }
}
