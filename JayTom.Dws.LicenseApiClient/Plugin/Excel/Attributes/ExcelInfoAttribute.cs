using System.Drawing;

namespace JayTom.Dws.LicenseApiClient.Plugin.Excel.Attributes {

    public class ExcelInfoAttribute : Attribute {
        public int Width { get; set; }
        public bool IsAutoSizeColumn { get; set; }
        public Color FillForegroundColor { get; set; }
        public Color FillBackgroundColor { get; set; }
        public bool IsEnumToInt { get; set; }
        public bool IsBooleanToInt { get; set; }
    }
}