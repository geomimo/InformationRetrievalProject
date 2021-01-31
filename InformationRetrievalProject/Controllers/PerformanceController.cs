using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using InformationRetrievalProject.Models;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.En;
using Lucene.Net.Analysis.Util;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers.Classic;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Microsoft.AspNetCore.Mvc;

namespace InformationRetrievalProject.Controllers
{
    public class PerformanceController : Controller
    {
        
        public ActionResult Performance(bool save = false)
        {
            ViewData["isOkapi"] = Data.Index.IsOkapi();
            ViewData["similarity"] = Data.Index.IsOkapi() ? "Okapi BM25" : "Default";
            return View(Data.Performance.Perfomance(save));
        }

        public ActionResult SetSimilarity(bool toOkapi)
        {
            if (toOkapi)
            {
                Data.Index.SetSimilarity(Data.Similarity.OkapiBM25);
            }
            else
            {
                Data.Index.SetSimilarity(Data.Similarity.Default);
            }

            return RedirectToAction("Performance");
        }      

    }
}
