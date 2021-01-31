using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InformationRetrievalProject.Models
{
    public class ConfusionMatrix
    {
        public int Id { get; set; }
        public int TP { get; set; }
        public int TN { get; set; }
        public int FP { get; set; }
        public int FN { get; set; }
        public float Recall { get; set; }
        public float Precision { get; set; }
        public History History { get; set; }
    }
}
