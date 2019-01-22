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
								var options = new Options(GetArg(config, "tagMask"));
								options.ServerName = GetArg(config, "server");
								options.UserName = GetArg(config, "user");
								options.Password = GetArg(config, "psw");
								DateTime.TryParse(GetArg(config, "start", StartOfDay(DateTime.Now.AddDays(-1))), out options.Start);
								DateTime.TryParse(GetArg(config, "end", StartOfDay(DateTime.Now)), out options.End);
								Enum.TryParse<DataCriteria.SamplingModeType>(GetArg(config, "samplingMode"), out options.SamplingMode);
								Enum.TryParse<DataCriteria.CalculationModeType>(GetArg(config, "calculationMode"), out options.CalculationMode);
								long.TryParse(GetArg(config, "intervalMicroseconds"), out options.IntervalMicroseconds);
								uint.TryParse(GetArg(config, "numberOfSamples"), out options.NumberOfSamples);
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
#if false
										PrintDataSet(ds);
#endif
								}
						}
						catch (Exception exc)
						{
								Log(exc.ToString());
								Environment.Exit(10);
						}
						_logFile.Flush();
						_logFile.Close();
						Environment.Exit(0);
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
														csvfile.WriteLine($"{r.Key};{r.Value.GetTime(i).ToUniversalTime().ToString("o")};{r.Value.GetQuality(i).PercentGood()};{r.Value.GetValue(i)}");
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
						Log("\t--samplingMode <CurrentValue | Interpolated | Trend | RawByTime | RawByNumber | Calculated | Lab | InterpolatedToRaw | TrendToRaw | LabToRaw | RawByFilterToggling | Trend2 | TrendToRaw2>");
						Log("\t--calculationMode <Average | StandardDeviation | Total | Minimum | Maximum | Count | RawAverage | RawStandardDeviation | RawTotal | MinimumTime | MaximumTime | TimeGood | StateCount | StateTime | OPCAnd | OPCOr | FirstRawValue | FirstRawTime | LastRawValue | LastRawTime | TagStats>");
						Log("\t[--start <start time, default: start of yesterday>]");
						Log("\t[--end <end time, default: start of today>]");
						Log("\t[--tagMask <tag mask, default *>]");
						Log("\t[--intervalMicroseconds <sample interval in 1/1000000 seconds>]");
						Log("\t[--numberOfSamples <number of samples, defualt 1>]");
						Log("\t[--out <output csv file>]");
						Log("\t[--size <MaxMessageSize>]");
				}

				private static string GetArg(IConfigurationRoot config, string key, string defaultValue = null)
				{
						if(!config.AsEnumerable().Any(a => a.Key == key))
						{
								return defaultValue;
						}
						return config.AsEnumerable().First(a => a.Key == key).Value;
				}

				private static string StartOfDay(DateTime date)
				{
						return date.ToString("yyyy-MM-dd");
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
						_historian = new ServerConnection(
								new ConnectionProperties
								{
										ServerHostName = options.ServerName,
										Username = options.UserName,
										Password = options.Password,
										ServerCertificateValidationMode = CertificateValidationMode.None,
										MaxReceivedMessageSize = Math.Max(1048576, options.MaxMessageSize),
										OpenTimeout = new TimeSpan(1, 0, 0),
										SendTimeout = new TimeSpan(1, 0, 0),
										ReceiveTimeout = new TimeSpan(1, 0, 0)
								});
						_historian.Connect();
						var tags = GetTagNames(_historian, options.TagMask);
						DataSet dataset = new DataSet();
						ItemErrors errors;
						int tagChunkSize = 1000;
						int offset = 0;
						while (offset < tags.Count())
						{
								Console.WriteLine("Exporting {0} tags at index {1} of {2}\r", tagChunkSize, offset, tags.Count());
								var tagChunk = tags.Skip(offset).Take(tagChunkSize).ToArray();
								offset += tagChunkSize;
								DataSet chunkDataSet;
								var parms = new DataQueryParams()
								{
										Criteria = new DataCriteria(tagChunk)
										{
												SamplingMode = options.SamplingMode,
												CalculationMode = options.CalculationMode,
												IntervalMicroseconds = options.IntervalMicroseconds,
												NumberOfSamples = options.NumberOfSamples,
												Start = options.Start,
												End = options.End
										},
										Fields = DataFields.Time | DataFields.Value | DataFields.Quality
								};
								_historian.IData.Query(ref parms, out chunkDataSet, out errors);
								if (errors.Count() > 0)
								{
										throw new Exception(string.Join(", ", errors.Select(e => $"{e.Key}: {e.Value}")));
								}
								dataset.AddRange(chunkDataSet);
						}

						return dataset;
				}

				private static string[] GetTagNames(ServerConnection connection, string tagMask)
				{
						List<Tag> tags = new List<Tag>();

						TagQueryParams tagQuery = new TagQueryParams { PageSize = 100 };
						tagQuery.Criteria.TagnameMask = tagMask;
						tagQuery.Categories = Tag.Categories.Basic;
						List<Tag> tagPage;
						while (connection.ITags.Query(ref tagQuery, out tagPage))
						{
								tags.AddRange(tagPage);
						}
						tags.AddRange(tagPage);
						return tags.Select(o => o.Name).ToArray();
				}
		}
}
