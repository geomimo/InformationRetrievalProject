using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InformationRetrievalProject.Controllers
{
    public class ArticleController : Controller
    {
        public IActionResult Index(int id)
        {
            Data.Document document = Data.Index.GetDocument(id);
            return View(document);
        }
    }
}
