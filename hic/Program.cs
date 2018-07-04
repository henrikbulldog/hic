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
            var builder = new ConfigurationBuilder();
            builder.AddCommandLine(args);
            var config = builder.Build();
            var options = new Options(Str2Array(GetArg(config, "tags")));
            options.ServerName = GetArg(config, "server");
            options.UserName = GetArg(config, "user");
            options.Password = GetArg(config, "psw");
            DateTime.TryParse(GetArg(config, "start"), out options.Criteria.Start);
            DateTime.TryParse(GetArg(config, "end"), out options.Criteria.End);
            Enum.TryParse<DataCriteria.SamplingModeType>(GetArg(config, "samplingMode"), out options.Criteria.SamplingMode);
            Enum.TryParse<DataCriteria.CalculationModeType>(GetArg(config, "calculationMode"), out options.Criteria.CalculationMode);
            long.TryParse(GetArg(config, "intervalMicroseconds"), out options.Criteria.IntervalMicroseconds);
            uint.TryParse(GetArg(config, "numberOfSamples"), out options.Criteria.NumberOfSamples);
            options.Out = GetArg(config, "out");

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
            Console.WriteLine("Usage: hic <options>");
            Console.WriteLine("Options:");
            Console.WriteLine("\t--server <server dns or ip>");
            Console.WriteLine("\t--user <user name>");
            Console.WriteLine("\t--psw <password>");
            Console.WriteLine("\t--tags <tag names>");
            Console.WriteLine("\t--start <start time>");
            Console.WriteLine("\t--end <end time>");
            Console.WriteLine("\t--samplingMode <CurrentValue | Interpolated | Trend | RawByTime | RawByNumber | Calculated | Lab | InterpolatedToRaw | TrendToRaw | LabToRaw | RawByFilterToggling | Trend2 | TrendToRaw2>");
            Console.WriteLine("\t--calculationMode <Average | StandardDeviation | Total | Minimum | Maximum | Count | RawAverage | RawStandardDeviation | RawTotal | MinimumTime | MaximumTime | TimeGood | StateCount | StateTime | OPCAnd | OPCOr | FirstRawValue | FirstRawTime | LastRawValue | LastRawTime | TagStats>");
            Console.WriteLine("\t--intervalMicroseconds <sample interval in 1/1000000 seconds>");
            Console.WriteLine("\t[--numberOfSamples <number of samples>]");
            Console.WriteLine("\t[--out <output csv file>]");
        }

        private static string GetArg(IConfigurationRoot config, string key)
        {
            return config.AsEnumerable().FirstOrDefault(a => a.Key == key).Value;
        }

        public static DataSet Query(Options options)
        {
            ServerConnection _historian;
            try
            {
                // Define connection and establish it
                _historian = new ServerConnection(new ConnectionProperties { ServerHostName = options.ServerName, Username = options.UserName, Password = options.Password, ServerCertificateValidationMode = CertificateValidationMode.None });
                _historian.Connect();
                var parms = new DataQueryParams()
                {
                    Criteria = options.Criteria
                };
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
