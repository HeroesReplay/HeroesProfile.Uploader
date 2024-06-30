using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HeroesProfile.Uploader.Models;

namespace HeroesProfile.Uploader.UI.Converters;

public class UploadColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value) {
            case UploadStatus.Success:
                return GetBrush("StatusUploadSuccessBrush");

            case UploadStatus.InProgress:
                return GetBrush("StatusUploadInProgressBrush");

            case UploadStatus.Duplicate:
            case UploadStatus.AiDetected:
            case UploadStatus.CustomGame:
            case UploadStatus.PtrRegion:
            case UploadStatus.TooOld:
                return GetBrush("StatusUploadNeutralBrush");

            case UploadStatus.Pending:
            case UploadStatus.UploadError:
            case UploadStatus.Incomplete:
            default:
                return GetBrush("StatusUploadFailedBrush");
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    private Brush GetBrush(string key)
    {
        return (SolidColorBrush)App.Current.Resources[key]! ?? throw new InvalidOperationException();
    }
}

public class UploadStatusEnumToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return (UploadStatus)value! switch {
            UploadStatus.Pending => "Pending",
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