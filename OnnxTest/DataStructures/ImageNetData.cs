using System;
using System.IO;
using System.Linq;
using System.Text;

using System.Linq;

using Microsoft.ML.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Collections.Generic;

namespace OnnxTest.DataStructures {

    public class ImageNetData {

        [LoadColumn(0)]
        public string ImagePath = string.Empty;

        [LoadColumn(1)]
        public string Label = string.Empty;

        public static IEnumerable<ImageNetData> ReadFromFile(string imageFolder) {
            return Directory
                .GetFiles(imageFolder)
                .Where(filePath => Path.GetExtension(filePath) != ".md")
                .Select(filePath => new ImageNetData { ImagePath = filePath, Label = Path.GetFileName(filePath) });
        }
    }
}