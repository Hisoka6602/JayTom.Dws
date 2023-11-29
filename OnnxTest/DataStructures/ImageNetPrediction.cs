using System;
using System.Linq;
using System.Text;
using Microsoft.ML.Data;

using Microsoft.ML.Data;

using System.Threading.Tasks;
using System.Collections.Generic;

namespace OnnxTest.DataStructures {

    public class ImageNetPrediction {

        [ColumnName("grid")]
        public float[] PredictedLabels;
    }
}