using Microsoft.Extensions.Configuration;
using Proficy.Historian.ClientAccess.API;
using System;
using System.IO;
using System.Linq;
using System.Net;

namespace hic
{
    class Program
    {
        static private StreamWriter _logFile = File.CreateText("hic-log.txt");

        static private void Log(string message)
        {
            Console.WriteLine(message);
            _logFile.WriteLine(message);
        }

        static void Main(string[] args)
        {
            try
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
                long.TryParse(GetArg(config, "size"), out options.MaxMessageSize);

                if (!options.Validate())
                {
                    Help();
                }
                else
                {
                    var ds = Query(options);

                    SaveDataSetToFile(ds, options.Out);
                    PrintDataSet(ds);
                }
            }
            catch (Exception exc)
            {
                Log(exc.ToString());
                Environment.Exit(10);
            }
            _logFile.Flush();
            _logFile.Close();
            Environment.Exit(1);
        }

        private static void SaveDataSetToFile(DataSet ds, string fileName)
        {
            if (ds == null)
            {
                throw new Exception("No data");
            }
            if (fileName != null)
            {
                Log($"Writing to file: { fileName }");
                var csvfile = File.CreateText(fileName);
                csvfile.WriteLine("Tag;TimeStamp;Quality;Value");
                try
                {
                    foreach (var r in ds)
                    {
                        for (int i = 0; i < r.Value.Count(); i++)
                        {
                            csvfile.WriteLine($"{r.Key};{r.Value.GetTime(i).ToUniversalTime().ToString("o")};{r.Value.GetQuality(i)};{r.Value.GetValue(i)}");
                        }
                    }
                }
                catch (Exception exc)
                {
                    Log($"Could not write dataset to file{fileName}. {exc}");
                    throw;
                }
                finally
                {
                    csvfile.Flush();
                    csvfile.Close();
                }
            }
        }

        private static void PrintDataSet(DataSet ds)
        {
            if (ds != null)
            {
                Log($"TotalSamples: { ds.TotalSamples }");

                foreach (var r in ds)
                {
                    Log($"Tag: {r.Key}");
                    for (int i = 0; i < Math.Min(r.Value.Count(), 3); i++)
                    {
                        Log($"Timestamp: {r.Value.GetTime(i).ToUniversalTime().ToString("o")} Quality: {r.Value.GetQuality(i)} Value: {r.Value.GetValue(i)}");
                    }
                }
            }
        }

        private static string[] Str2Array(string str)
        {
            if (str != null)
                return str.Split(',');
            return new string[] { };
        }

        private static void Help()
        {
            Log("Usage: hic <options>");
            Log("Options:");
            Log("\t--server <server dns or ip>");
            Log("\t--user <user name>");
            Log("\t--psw <password>");
            Log("\t--tags <tag names>");
            Log("\t--start <start time>");
            Log("\t--end <end time>");
            Log("\t--samplingMode <CurrentValue | Interpolated | Trend | RawByTime | RawByNumber | Calculated | Lab | InterpolatedToRaw | TrendToRaw | LabToRaw | RawByFilterToggling | Trend2 | TrendToRaw2>");
            Log("\t--calculationMode <Average | StandardDeviation | Total | Minimum | Maximum | Count | RawAverage | RawStandardDeviation | RawTotal | MinimumTime | MaximumTime | TimeGood | StateCount | StateTime | OPCAnd | OPCOr | FirstRawValue | FirstRawTime | LastRawValue | LastRawTime | TagStats>");
            Log("\t--intervalMicroseconds <sample interval in 1/1000000 seconds>");
            Log("\t[--numberOfSamples <number of samples>]");
            Log("\t[--out <output csv file>]");
            Log("\t[--size <MaxMessageSize>]");
        }

        private static string GetArg(IConfigurationRoot config, string key)
        {
            return config.AsEnumerable().FirstOrDefault(a => a.Key == key).Value;
        }

        public static DataSet Query(Options options)
        {
            ServerConnection _historian;
            var xx = new ConnectionProperties
            {
                ServerHostName = options.ServerName,
                Username = options.UserName,
                Password = options.Password,
                ServerCertificateValidationMode = CertificateValidationMode.None

            };

            // Define connection and establish it
            _historian = new ServerConnection(
                new ConnectionProperties
                {
                    ServerHostName = options.ServerName,
                    Username = options.UserName,
                    Password = options.Password,
                    ServerCertificateValidationMode = CertificateValidationMode.None,
                    MaxReceivedMessageSize = Math.Max(1048576, options.MaxMessageSize)
                });

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

    }
}
