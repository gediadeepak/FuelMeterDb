namespace FuelMeter.Core.Services.Interfaces;

public interface IMeterCamera
{
    bool IsSupported { get; }
    Task<Stream?> CaptureAsync();
}
