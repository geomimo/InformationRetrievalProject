using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using InformationRetrievalProject.Models;
using Lucene.Net.Store;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;
using Lucene.Net.Index;
using System.IO;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Core;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Search;
using Lucene.Net.QueryParsers.Classic;
using System.Text.RegularExpressions;
using Lucene.Net.Search.Highlight;
using InformationRetrievalProject.Data;

namespace InformationRetrievalProject.Controllers
{
    public class HomeController : Controller
    {
        static List<Document> results = new List<Document>();
       
        public HomeController()
        {
            Data.Index.Initialize();
        }

        public IActionResult Index()
        {
            if(TempData["searchFor"] != null)
            {
                ViewData["searchFor"] = TempData["searchFor"];
            }

            ViewData["similarity"] = Data.Index.IsOkapi() ? "Okapi BM25" : "Default";
            Document[] documents = new Document[results.Count()];
            results.CopyTo(documents);
            results.Clear();
            return View(documents.ToList());
        }

        
        public ActionResult Search(string searchPhrase, bool relevant = false, int id = -1)
        {
            results.Clear();
            if (searchPhrase != "")
            {
                string searchedFor = Data.Index.Search(searchPhrase, results, 10, relevant, id);
                TempData["searchFor"] = searchedFor;
            }
            
            return RedirectToAction(nameof(Index));
        }
    

        public ActionResult SearchRelevant(int id)
        {
            return RedirectToAction(nameof(Search), new { searchPhrase = "", relevant = true, id = id });
        }


 
    }
}
