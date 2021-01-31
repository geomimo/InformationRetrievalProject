using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace InformationRetrievalProject.Models
{
    public class History
    {
        public float[] Recall { get; set; }
        public float[] Precision { get; set; }

        public History(int k)
        {
            Recall = new float[k];
            Precision = new float[k];
        }
    }
}
