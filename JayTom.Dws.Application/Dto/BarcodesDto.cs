using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using JayTom.Dws.Domain.Dto.CloudDto;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JayTom.Dws.Application.Dto {

    public class BarcodesDto {

        /// <summary>
        /// 条数
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// 实体
        /// </summary>
        public List<BarcodesInfoDto> BarCodes { get; set; } = new();
    }

    public class BarcodesInfoDto {
        public long Id { get; set; }

        /// <summary>
        /// 时间戳Id
        /// </summary>
        public long TimestampedGuid { get; set; }

        /// <summary>
        /// 条码
        /// </summary>
        public string Barcode { get; set; } = string.Empty;

        /// <summary>
        /// 节点
        /// </summary>
        public List<ScanNodeInfoDto> ScanNodeInfos { get; set; } = new();
    }

    public class ScanNodeInfoDto {
        /// <summary>
        /// 节点名称
        /// </summary>

        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 节点扫描时间
        /// </summary>

        public DateTime ScanTime { get; set; }

        /// <summary>
        /// 说明
        /// </summary>

        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 绑定通道
        /// </summary>
        public NvrCameraBindingDto NvrCameraBindingInfo { get; set; } = new();

        /// <summary>
        /// 图片
        /// </summary>
        public List<BarcodeImageInfoDto> BarcodeImageInfos = new();
    }

    public class BarcodeImageInfoDto {

        /// <summary>
        /// 图片名称
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 图片路径
        /// </summary>
        public string Path { get; set; } = string.Empty;

        /// <summary>
        /// 图片类型(0=扫码图、1=全景图)
        /// </summary>
        public int ImageType { get; set; }

        /// <summary>
        /// 相机序列号
        /// </summary>
        public string CameraSerialNumber { get; set; } = string.Empty;

        /// <summary>
        /// 相机名称
        /// </summary>
        public string CameraName { get; set; } = string.Empty;
    }
}