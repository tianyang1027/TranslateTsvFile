
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace TranslateTsvFile
{
    internal class Program
    {
        private const string entityNameSpec = "{0}";

        private const string seeMoreSpec = "{1}";

        private static string[] langs = { "de", "es", "pt", "it", "ja", "fr", "ar", "id", "nl", "ru", "zh" };

        static void Main(string[] args)
        {
            FileInfo[] files = new DirectoryInfo(@"C:\Users\v-yangtian\Downloads\translate_input").GetFiles("*.tsv");
            var tasks = new Task[files.Length];
            for (int i = 0; i < files.Length; i++)
            {
                var file = files[i];
                tasks[i] = Task.Run(async () =>
                {
                    HashSet<string> lines = new HashSet<string>(File.ReadLines(file.FullName));
                    var translateColumn = GetTranslateColumn(file.FullName);
                    string fileName = file.Name.Replace(file.Extension, string.Empty);
                    int index_Hash_EntityName = lines.Any() ? Array.IndexOf(lines.First().Split('\t'), "Hash_EntityName") : 0;
                    foreach (string lang in langs)
                    {
                        var folder = $@"c:\translate_output\{fileName}";
                        if (!Directory.Exists(folder))
                        {
                            Directory.CreateDirectory(folder);
                        }
                        TextWriter writer = new StreamWriter($@"{folder}\{lang}.tsv");
                        foreach (string line in lines)
                        {
                            var cells = line.Split('\t');
                            List<string> newCells = new List<string>();
                            var value_Hash_EntityName = cells.GetValue(index_Hash_EntityName).ToString();
                            for (int j = 0; j < cells.Length; j++)
                            {
                                var cell = cells[j];
                                if (translateColumn.Select(x => x.Index).Contains(j) && !translateColumn.Select(x => x.ColumnName).Contains(cell))
                                {
                                    var seeMore = "&nbsp;See more";
                                    string inputTranslateText = cell.Contains(value_Hash_EntityName) ? cell.Replace(value_Hash_EntityName, entityNameSpec) : cell;
                                    inputTranslateText = inputTranslateText.Contains(seeMore) ? inputTranslateText.Replace(seeMore, seeMoreSpec) : inputTranslateText;
                                    string outputTranslateText = await TranslateText("en", lang, inputTranslateText);
                                    cell = outputTranslateText.Contains(entityNameSpec) ? outputTranslateText.Replace(entityNameSpec, value_Hash_EntityName) : outputTranslateText;
                                    cell = cell.Contains(seeMoreSpec) ? cell.Replace(seeMoreSpec, seeMore) : cell;
                                }
                                newCells.Add(cell);
                            }
                            writer.WriteLine(string.Join("\t", newCells.ToArray()));
                        }
                        writer.Close();
                    }
                });
            }
            Task.WaitAll(tasks);
            Console.WriteLine("output translate file success!");
            Console.ReadKey();
        }

        private static async Task<string> TranslateText(string languageFrom, string languageTo, string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            string url = string.Format("https://translate.googleapis.com/translate_a/single?client=gtx&sl={0}&tl={1}&dt=t&q={2}", languageFrom, languageTo, HttpUtility.UrlEncode(input));
            HttpClient httpClient = new HttpClient();
            string result = await httpClient.GetStringAsync(url);
            var jsonData = JsonConvert.DeserializeObject<List<dynamic>>(result);
            if (jsonData != null && jsonData.Any())
            {
                var translationItems = jsonData[0];
                string translation = "";
                foreach (object item in translationItems)
                {
                    IEnumerable translationLineObject = item as IEnumerable;
                    IEnumerator translationLineString = translationLineObject.GetEnumerator();
                    translationLineString?.MoveNext();
                    translation += string.Format(" {0}", Convert.ToString(translationLineString.Current));
                }

                if (translation.Length > 1) { translation = translation.Substring(1); };
                return translation;
            }
            return string.Empty;
        }

        private static List<ColumnInfo> GetTranslateColumn(string path)
        {
            HashSet<string> lines = new HashSet<string>(File.ReadAllLines(path));
            string[] cells = lines.First().Split('\t');
            return new List<ColumnInfo>
            {
               new ColumnInfo
               {
                  Index = Array.IndexOf(cells, "@RealTimeAuditComment"),
                  ColumnName = "@RealTimeAuditComment"
               },
               new ColumnInfo
               {
                  Index = Array.IndexOf(cells, "Hash_Description"),
                  ColumnName = "Hash_Description"
               }
            };
        }

        internal class ColumnInfo
        {
            public int Index { get; set; }

            public string ColumnName { get; set; }
        }
    }
}
