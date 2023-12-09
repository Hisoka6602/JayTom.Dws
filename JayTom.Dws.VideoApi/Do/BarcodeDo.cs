using System.ComponentModel.DataAnnotations;

namespace JayTom.Dws.VideoApi.Do {

    public class BarcodeDo {

        [MaxLength(40, ErrorMessage = "条码长度不能超过40")]
        public string? BarCode { get; set; }

        public DateTime? NodeStartDateTime { get; set; }
        public DateTime? NodeEndDateTime { get; set; }
        public string? NodeName { get; set; }
        public string? CameraSerialNumber { get; set; }
        public string? CameraName { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "PageIndex参数超出限制范围!")]
        public int PageIndex { get; set; } = 0;

        [Range(1, 1000, ErrorMessage = "PageSize参数超出限制范围!")]
        public int PageSize { get; set; } = 1000;
    }
}