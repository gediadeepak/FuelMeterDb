using FuelMeter.Core.Services.Interfaces;
using Microsoft.Maui.Media;

namespace FuelMeter.Services;

public sealed class MeterCamera : IMeterCamera
{
    public bool IsSupported => MediaPicker.Default.IsCaptureSupported;

    public async Task<Stream?> CaptureAsync()
    {
        var photo = await MainThread.InvokeOnMainThreadAsync(() => MediaPicker.Default.CapturePhotoAsync(new()
        {
            Title = "Take a meter photo",
            MaximumWidth = 2048,
            MaximumHeight = 2048,
            CompressionQuality = 85,
            RotateImage = true,
            PreserveMetaData = false
        }));
        return photo is null ? null : await photo.OpenReadAsync();
    }
}
