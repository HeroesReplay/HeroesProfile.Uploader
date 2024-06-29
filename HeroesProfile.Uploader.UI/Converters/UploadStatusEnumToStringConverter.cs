using System;
using System.Globalization;
using Avalonia.Data.Converters;
using HeroesProfile.Uploader.Models;

namespace HeroesProfile.Uploader.UI.Converters;

public class UploadStatusEnumToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (UploadStatus)value! switch {
            UploadStatus.Pending => nameof(UploadStatus.Pending),
            UploadStatus.Success => nameof(UploadStatus.Success),
            UploadStatus.InProgress => "In Progress",
            UploadStatus.UploadError => "Upload Error",
            UploadStatus.Duplicate => nameof(UploadStatus.Duplicate),
            UploadStatus.AiDetected => "AI Detected",
            UploadStatus.CustomGame => "Custom Game",
            UploadStatus.PtrRegion => "PTR Region",
            UploadStatus.Incomplete => nameof(UploadStatus.Incomplete),
            UploadStatus.TooOld => "Too old",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}