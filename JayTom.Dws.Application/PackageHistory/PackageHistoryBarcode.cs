namespace JayTom.Dws.Application.PackageHistory;

/// <summary>历史条码读模型。</summary>
public sealed record PackageHistoryBarcode(string Barcode, DateTime ScanTime, string SerialNumber);
