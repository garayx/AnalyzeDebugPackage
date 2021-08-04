using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

namespace AnalyzeDebugPackage
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var packageLocation = "";
            var expConverter = new ExpandoObjectConverter();
            var countOfIndexes = 0l;
            var countOfDocuments = 0l;
            var countOfAttachments = 0l;
            var countOfUniqueAttachments = 0l;

            var dirs = System.IO.Directory.GetDirectories(packageLocation).ToList();
            dirs.Remove(Path.Combine(packageLocation, "server-wide"));

            var first = true;
            foreach (var dir in dirs)
            {
                using (StreamReader r = new StreamReader(Path.Combine(dir, "stats.json")))
                {
                    string json = r.ReadToEnd();
                    IDictionary<string, object> o = JsonConvert.DeserializeObject<ExpandoObject>(json, expConverter);

                    var indexesCount = (long) o["CountOfIndexes"];
                    var docsCount = (long)o["CountOfDocuments"];
                    var attachmentsCount = (long)o["CountOfAttachments"];
                    var uniqueAttachmentsCount = (long)o["CountOfUniqueAttachments"];
                    countOfIndexes += indexesCount;
                    countOfDocuments += docsCount;
                    countOfAttachments += attachmentsCount;
                    countOfUniqueAttachments += uniqueAttachmentsCount;
                    Console.WriteLine($"{dir}{Environment.NewLine}Docs: {docsCount}{Environment.NewLine}Indexes: {indexesCount}{Environment.NewLine}Attachments: {attachmentsCount} ({uniqueAttachmentsCount})");
                }
            }

            Console.WriteLine($"{Environment.NewLine}Totals:");
            Console.WriteLine($"Count of DBs: {dirs.Count}");
            Console.WriteLine($"CountOfIndexes: {countOfIndexes}");
            Console.WriteLine($"CountOfDocuments: {countOfDocuments}");
            Console.WriteLine($"CountOfAttachments: {countOfAttachments} (CountOfUniqueAttachments: {countOfUniqueAttachments})");


            var buildFile = Path.Combine(packageLocation, "server-wide", "build.version.json");
            if (File.Exists(buildFile))
            {
                var jObject = JObject.Parse(File.ReadAllText(buildFile));
                var version = jObject.Value<string>("FullVersion");
                Console.WriteLine("- -- - -- - - - - - - - - - -");
                Console.WriteLine($"Server Version: {version}");
            }

            var slowDiskWrites = 0;
            foreach (var f in Directory.GetFiles(packageLocation, "io-metrics.json", SearchOption.AllDirectories))
            {
                JObject jObject = JObject.Parse(File.ReadAllText(f));
                var foo = jObject.Value<JArray>("Environments").SelectMany(x => x.Value<JArray>("Files")).SelectMany(x => x.Value<JArray>("Recent"))
                    .Where(x => x.Value<double>("Duration") > 1000).Where(x=>x.Value<string>("Type") == "JournalWrite");

                bool wrote = false;

                foreach (var item in foo)
                {
                    if (!wrote)
                    {
                        Console.WriteLine("- -- - -- - - - - - - - - - -");
                        Console.WriteLine(f);
                    }
                    wrote = true;
                    Console.WriteLine(item);

                    slowDiskWrites++;
                }
            }

            Console.WriteLine("Total slow writes: " + slowDiskWrites);
            var adminIoFile = Path.Combine(packageLocation, "server-wide", "admin.io-metrics.json");
            if (File.Exists(adminIoFile))
            {
                JObject jObject = JObject.Parse(File.ReadAllText(adminIoFile));
                var foo = jObject.Value<JArray>("Environments").SelectMany(x => x.Value<JArray>("Files")).SelectMany(x => x.Value<JArray>("Recent"))
                    .Where(x => x.Value<double>("Duration") > 1000).Where(x => x.Value<string>("Type") == "JournalWrite");

                bool wrote = false;

                foreach (var item in foo)
                {
                    if (!wrote)
                    {
                        Console.WriteLine("- -- - -- - - - - - - - - - -");
                        Console.WriteLine(adminIoFile);
                    }
                    wrote = true;
                    Console.WriteLine(item);
                }
            }


            //var adminLowMemFile = Path.Combine(packageLocation, "server-wide", "admin.memory.low-mem-log.json");
            //if (File.Exists(adminIoFile))
            //{
            //    var jObject = JObject.Parse(File.ReadAllText(adminIoFile));

            //}

            Console.WriteLine();
        }
    }
}
