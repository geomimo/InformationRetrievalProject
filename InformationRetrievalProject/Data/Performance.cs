using InformationRetrievalProject.Models;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace InformationRetrievalProject.Data
{
    public static class Performance
    {
        static string dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Index");
        static string stopPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "common_words");
        static string docPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "cacm.all");
        static string quePath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "query.text");
        static string relationPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "qrels.text");
        static string resultsPath = System.IO.Path.Combine(Environment.CurrentDirectory, "results", "history");
        static string[] otherTags = { ".A", ".N", ".C", ".E", ".Y", ".J", ".L", ".O", ".M" };


      
        //BM25 f1: 18.9105778 prec:20.25 rec:17.7373562 3.5 3 1.5
        public static List<ConfusionMatrix> Perfomance(bool save = false)
        {
            string[] queries = GetQueries();
            Dictionary<int, List<int>> relevant = GetRelevant();
           
           
            List<ConfusionMatrix> confusionList = new List<ConfusionMatrix>();

            for (int i = 0; i < queries.Length; i++)
            {
                if (!relevant.ContainsKey(i + 1)) continue;

                List<Document> results = new List<Document>();

                Index.Search(queries[i], results);

                int tp = 0;
                int fp = 0;
                History history = new History(8);
                int j = 0;
                foreach (var doc in results)
                {
                    if (relevant[i + 1].Contains(doc.Id))
                    {
                        tp += 1;
                    }
                    else
                    {
                        fp += 1;
                    }
                    history.Precision[j] = tp / (float)(tp + fp);
                    history.Recall[j] = tp / (float)relevant[i + 1].Count();
                    j++;
                }
                int fn = relevant[i + 1].Count() - tp;
                int tn = relevant.Count() - relevant[i + 1].Count() - fp;

                confusionList.Add(new ConfusionMatrix()
                {
                    Id = i + 1,
                    TP = tp,
                    TN = tn,
                    FP = fp,
                    FN = fn,
                    Recall = (float)tp / (float)(tp + fn),
                    Precision = (float)tp / (float)(tp + fp),
                    History = history
                });
 
            }

            if (save)
            {
                SaveResults(confusionList);
            }

            return confusionList;

        }

        private static string[] GetQueries()
        {
            string[] data = System.IO.File.ReadAllLines(quePath);

            string[] queries = new string[64];

            string curField = "";
            string value = "";
            int i = 0;
            foreach (var line in data)
            {
                if (line.Contains(".W") && line.Length == 2)
                {
                    curField = ".W";
                }
                else if (otherTags.Contains(line))
                {
                    if (curField == ".W")
                    {
                        queries[i] = value;
                        i++;
                        value = "";
                    }
                    curField = "other";
                }
                else if (curField == ".W")
                {
                    value += line + " ";
                }
            }

            return queries;
        }     

        private static Dictionary<int, List<int>> GetRelevant()
        {
            string[] data = System.IO.File.ReadAllLines(relationPath);

            Dictionary<int, List<int>> relevant = new Dictionary<int, List<int>>();
            List<int> relList;
            foreach (var line in data)
            {
                int id = Int32.Parse(line.Split(" ")[0]);
                int value = Int32.Parse(line.Split(" ")[1]);
                if (!relevant.ContainsKey(id))
                {
                    relList = new List<int>();
                    relList.Add(value);
                    relevant.Add(id, relList);
                }
                else
                {
                    relevant[id].Add(value);
                }
            }

            return relevant;
        }

        private static void SaveResults(List<ConfusionMatrix> confusionList)
        {
            string[] files = System.IO.Directory.GetFiles(resultsPath);

            string data = "";
            data += "ID,TP,TN,FP,FN,RECALL,PRECISION";
            for (int i = 0; i < 8; i++)
            {
                data += ",R" + i.ToString();
            }
            for (int i = 0; i < 8; i++)
            {
                data += ",P" + i.ToString();
            }
            data += "\n";
            foreach (var matrix in confusionList)
            {

                data += matrix.Id.ToString() + "," + matrix.TP.ToString() + "," +
                        matrix.TN.ToString() + "," + matrix.FP.ToString() + "," +
                        matrix.FN.ToString() + "," + matrix.Recall.ToString() + "," +
                        matrix.Precision.ToString();
                foreach (float value in matrix.History.Recall)
                {
                    data += "," + value.ToString();
                }
                foreach (float value in matrix.History.Precision)
                {
                    data += "," + value.ToString();
                }
                data += "\n";
            }
            string filename = Index.IsOkapi() ? "BM25.csv" : "default.csv";
            System.IO.File.WriteAllText(System.IO.Path.Combine(resultsPath, filename), data);
        }
    }
}
