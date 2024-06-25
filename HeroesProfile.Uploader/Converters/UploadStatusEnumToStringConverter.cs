using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HeroesProfile.Uploader.Core.Enums;

namespace HeroesProfile.Uploader.Converters;

public class UploadStatusEnumToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (UploadStatus)value! switch {
            UploadStatus.None => "None",
            UploadStatus.Success => "Success",
            UploadStatus.InProgress => "In Progress",
            UploadStatus.UploadError => "Upload Error",
            UploadStatus.Duplicate => "Duplicate",
            UploadStatus.AiDetected => "AI Detected",
            UploadStatus.CustomGame => "Custom Game",
            UploadStatus.PtrRegion => "PTR Region",
            UploadStatus.Incomplete => "Incomplete",
            UploadStatus.TooOld => "Too old",
            _ => throw new ArgumentOutOfRangeException(nameof(value), value, null)
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}