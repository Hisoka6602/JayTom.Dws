using JayTom.Dws.Plugin.Tcp;

namespace JayTom.Dws.Plugin.Device.GrayscaleDevice;

/// <summary>
/// Hardware adapter for the grayscale parcel-position sensor.
/// Result models live in Abstractions so application workflows do not depend on this adapter project.
/// </summary>
public interface IGrayscaleDevice : ITcpOperations {
    event EventHandler<GrayscaleResult> ParcelLocationReceived;

    event EventHandler ParcelLocationNotReceived;

    Task<bool> SendCarNumber(int carNumber, CancellationToken token = default);

    Task<GrayscaleResult> SendCarNumber(
        int carNumber,
        int timeOut,
        CancellationToken token = default);

    void SetRectangleSizes(
        Coordinates attachmentRectangle,
        Coordinates mainRectangle,
        int additionalBoxSpacePercentage = 20,
        int minSendInterval = 300);

    void SetRegionCarCount(int regionCarCount);

    void SetDirectionReversed(bool isReversed);

    void SetCircularArrayCarCount(int carCount, int offset);

    int IncreaseCarCount(int carNum, int additionalCarCount);
}
