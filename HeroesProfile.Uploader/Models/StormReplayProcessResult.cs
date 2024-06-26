using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using HeroesProfile.Uploader.Core.Enums;

namespace HeroesProfile.Uploader.Models;

public sealed class StormReplayProcessResult : INotifyPropertyChanged
{
    public int Count { get; set; }
    public UploadStatus UploadStatus { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}