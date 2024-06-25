using HeroesProfile.Uploader.Common;

using System;
using System.Windows;

namespace Heroesprofile.Uploader.Windows.UIHelpers;

public class FlagsConverter : GenericValueConverter<Enum, bool, Enum>
{
    protected override bool Convert(Enum value, Enum parameter)
    {
        return value.HasFlag(parameter);
    }

    protected override Enum ConvertBack(bool value, Enum parameter)
    {                        
        var val =  App.Current.AppSettings.DeleteAfterUpload;

        if (value) {
            val |= (DeleteFiles)parameter;
        } else {
            val &= ~(DeleteFiles)parameter;
        }
        return val;
    }
}
