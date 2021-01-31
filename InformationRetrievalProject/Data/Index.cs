using Lucene.Net.Analysis;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Search.Highlight;
using Lucene.Net.Search.Similarities;
using Lucene.Net.Store;
using Lucene.Net.Util;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace InformationRetrievalProject.Data
{

    public static class Index
    {
        static FieldType titleType;
        static FieldType contentType;
        static FieldType authorType;
        static string[] fields = { "title", "content", "author" };
        static string dirPath = System.IO.Path.Combine(Environment.CurrentDirectory, "Index");
        static string stopPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "common_words");
        static string docPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "cacm.all");
        static string quePath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "query.text");
        static string relPath = System.IO.Path.Combine(Environment.CurrentDirectory, "cacm", "qrels.text");
        static string[] otherTags = { ".C", ".D", ".E", ".L", ".X", ".S", ".H", ".O", ".Y", ".Q", ".M", ".K", ".U", ".G", ".V", ".F", ".B", ".N", ".R", ".P" };

        
        static Dictionary<string, float> BM25Parameters = new Dictionary<string, float>
        {
            {"k1", 1.25f },
            {"b", 0.0f }
        };

        static bool UseOBM25 = false;

        public static bool IsOkapi()
        {
            return UseOBM25;
        }

        public static string SetSimilarity(Similarity sim)
        {
            UseOBM25 = sim.Equals(Similarity.OkapiBM25);
            return Initialize();
        }
        
        public static string Initialize()
        {
            using var directory = FSDirectory.Open(dirPath);

            var analyzer = GetAnalyzer();
            var indexConfig = new IndexWriterConfig(LuceneVersion.LUCENE_48, analyzer);
            indexConfig.OpenMode = OpenMode.CREATE;

            if (UseOBM25)
            {
                // k1: 1.25 b: 0 f1:17.4878521 prec: 19.125 rec:16.1088924
                indexConfig.Similarity = new BM25Similarity(BM25Parameters["k1"], BM25Parameters["b"]);
            }
            

            using var writer = new IndexWriter(directory, indexConfig);

            var watch = Stopwatch.StartNew();
            IndexDocs(writer, docPath);
            writer.Commit();
            directory.ClearLock(IndexWriter.WRITE_LOCK_NAME);
            watch.Stop();

            return watch.Elapsed.TotalSeconds.ToString();
        }
      
        public static string Search(string searchPhrase, List<Document> documents, int n_results = 8, bool relevant = false, int id = -1)
        {
            using var directory = FSDirectory.Open(dirPath);
            var dirReader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(dirReader);

            if (UseOBM25)
            {
                searcher.Similarity = new BM25Similarity(BM25Parameters["k1"], BM25Parameters["b"]);
            }

            var analyzer = GetAnalyzer();

            var boosts = new Dictionary<string, float>
            {
                {"title", 3.5f },
                {"content", 3.0f },
                {"author", 1.0f }
            };
            //3.5 3 1 f1: 18.80524 prec: 17.8181839 rec:19.9080658
            // 8 f1: 18.83765 prec: 20.125 rec: 17.7051
            var parser = new MultiFieldQueryParser(LuceneVersion.LUCENE_48, fields, analyzer, boosts);

            string resultsForText = "";
            if (relevant && id != -1)
            {
                Lucene.Net.Documents.Document docToSearch = searcher.Doc(id - 1);
                searchPhrase = docToSearch.Get("title") + " " + docToSearch.Get("content") + " " + docToSearch.Get("author");
                resultsForText = docToSearch.Get("title");
            }
            else
            {
                resultsForText = searchPhrase;
            }

            string replacedSearchPhrase = ReplaceSpecialCharacters(searchPhrase);
            Query query = parser.Parse(replacedSearchPhrase);
            TopDocs hits;
            if (relevant)
            {
                // + 1 in order to pop the 1 hit aka the document which is seached for relevants
                hits = searcher.Search(query, n_results + 1); 
            }
            else
            {
                hits = searcher.Search(query, n_results);
            }

            foreach (var hit in hits.ScoreDocs)
            {

                var hitDoc = searcher.Doc(hit.Doc);
                var doc = new Document
                {
                    Id = hit.Doc + 1,
                    Title = hitDoc.Get("title"),
                    Content = hitDoc.Get("content"),
                    Author = hitDoc.Get("author")
                };

                documents.Add(doc);
            }

            HighlightDocuments(documents, hits, query, searcher, analyzer);

            if (relevant)
            {
                documents.RemoveAll(doc => doc.Id == id);
            }            

            return resultsForText;
        }

        public static Document GetDocument(int id)
        {
            using var directory = FSDirectory.Open(dirPath);
            var dirReader = DirectoryReader.Open(directory);
            var searcher = new IndexSearcher(dirReader);

            var doc = searcher.Doc(id - 1);

            return new Document
            {
                Id = id,
                Title = doc.Get("title"),
                Content = doc.Get("content"),
                Author = doc.Get("author")
            };

        }

        private static string ReplaceSpecialCharacters(string text)
        {
            MatchCollection matches = Regex.Matches(text, @"[\+\-\|\(\)\{\}\[\]\^*\?\\&:~/\""]");
            foreach (Match match in matches)
            {
                text = Regex.Replace(text, "\\" + match.Value, "\\" + match.Value);
            }

            return text;
        }
       
        private static Analyzer GetAnalyzer()
        {
            var stopwords = new CharArraySet(LuceneVersion.LUCENE_48, System.IO.File.ReadAllLines(stopPath), false);
            return new EnglishAnalyzer(LuceneVersion.LUCENE_48, stopwords);
        }

        private static void IndexDocs(IndexWriter writer, string path)
        {
            string[] data = System.IO.File.ReadAllLines(path);

            ConfigTypes();
            Lucene.Net.Documents.Document doc = null;
            string curDocField = "";
            string value = "";

            foreach (string line in data)
            {
                if (line.Contains(".I"))
                {
                    if (curDocField == "other")
                    {
                        writer.AddDocument(doc);
                    }
                    doc = new Lucene.Net.Documents.Document();
                    value = "";
                }
                else if (line.Contains(".T") && line.Length == 2)
                {
                    doc = AddField(curDocField, value, doc);
                    value = "";
                    curDocField = ".T";
                }
                else if (line.Contains(".W") && line.Length == 2)
                {
                    doc = AddField(curDocField, value, doc);
                    value = "";
                    curDocField = ".W";
                }
                else if (line.Contains(".A") && line.Length == 2)
                {
                    doc = AddField(curDocField, value, doc);
                    value = "";
                    curDocField = ".A";
                }
                else if (otherTags.Contains(line) && line.Length == 2)
                {
                    doc = AddField(curDocField, value, doc);
                    value = "";
                    curDocField = "other";
                }
                else if (curDocField == ".T" || curDocField == ".W" || curDocField == ".A")
                {
                    value += line + " ";                
                }
            }
        }

        private static void ConfigTypes()
        {

            titleType = new FieldType();
            titleType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            titleType.IsIndexed = true;
            titleType.IsStored = true;
            titleType.IsTokenized = true;
            titleType.StoreTermVectors = true;
            titleType.StoreTermVectorOffsets = true;
            titleType.StoreTermVectorPayloads = true;
            titleType.StoreTermVectorPositions = true;

            contentType = new FieldType();
            contentType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS_AND_OFFSETS;
            contentType.IsIndexed = true;
            contentType.IsStored = true;
            contentType.IsTokenized = true;
            contentType.StoreTermVectors = true;
            contentType.StoreTermVectorOffsets = true;
            contentType.StoreTermVectorPayloads = true;
            contentType.StoreTermVectorPositions = true;


            authorType = new FieldType();
            authorType.IndexOptions = IndexOptions.DOCS_AND_FREQS_AND_POSITIONS; // Stop gettin errors
            authorType.IsIndexed = true;
            authorType.IsStored = true;
            authorType.IsTokenized = true;
            authorType.StoreTermVectors = false;
            authorType.OmitNorms = true;

        }

        private static Lucene.Net.Documents.Document AddField(string docField, string value, Lucene.Net.Documents.Document doc)
        {
            switch (docField)
            {
                case ".T":
                    doc.Add(new Field("title", value, titleType));
                    break;
                case ".W":
                    Field contentField = new Field("content", value, contentType);
                    doc.Add(contentField);
                    break;
                case ".A":
                    doc.Add(new Field("author", value, authorType));
                    break;
                default:
                    break;
            }
            return doc;
        }

        private static void HighlightDocuments(List<Document> documents, TopDocs hits, Query query, IndexSearcher searcher, Analyzer analyzer)
        {
            var htmlFormatter = new SimpleHTMLFormatter("<u><b>", "</b></u>");
            Highlighter highlighter = new Highlighter(htmlFormatter, new QueryScorer(query));


            for (int i = 0; i < hits.ScoreDocs.Length && i < 10; i++)
            {
                int id = hits.ScoreDocs[i].Doc;

                string titleHighlightedext = HighlightText(id, "title", highlighter, searcher, analyzer);
                string contentHighlightedText = HighlightText(id, "content", highlighter, searcher, analyzer);
                string authorHighlightedText = HighlightText(id, "author", highlighter, searcher, analyzer);

                if (titleHighlightedext != "")
                {
                    documents[i].Title = titleHighlightedext;

                }
                if (contentHighlightedText != "")
                {
                    documents[i].Content = contentHighlightedText;

                }
                if (authorHighlightedText != "")
                {
                    documents[i].Author = authorHighlightedText;

                }
            }

        }

        private static string HighlightText(int id, string field, Highlighter highlighter, IndexSearcher searcher, Analyzer analyzer)
        {
            Lucene.Net.Documents.Document doc = searcher.Doc(id);
            var highlightedText = "";
            var text = doc.Get(field);
            if (text == null)
            {
                return String.Empty;
            }
            TokenStream tokenStream = TokenSources.GetAnyTokenStream(searcher.IndexReader, id, field, analyzer);

            highlightedText = highlighter.GetBestFragments(tokenStream, text, 2, "...");

            return highlightedText;
        }
    
    }
}
