using System;
using System.Globalization;
using System.Windows.Data;

namespace Heroesprofile.Uploader.Windows.UIHelpers;

public abstract class GenericValueConverter<TV, T, TP> : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //if (value.GetType() != typeof(V)) throw new ArgumentException(GetType().Name + ".Convert: value type not " + typeof(V).Name);
        //if (targetType != typeof(T)) throw new ArgumentException(GetType().Name + ".Convert: target type not " + typeof(T).Name);
        //if (parameter != null) throw new ArgumentException(GetType().Name + ".Convert: binding contains unexpected parameter");
        return Convert((TV)value, (TP)parameter);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //if (value.GetType() != typeof(T)) throw new ArgumentException(GetType().Name + ".ConvertBack: value type not " + typeof(T).Name);
        //if (targetType != typeof(V)) throw new ArgumentException(GetType().Name + ".ConvertBack: target type not " + typeof(V).Name);
        //if (parameter != null) throw new ArgumentException(GetType().Name + ".Convert: binding contains unexpected parameter");
        return ConvertBack((T)value, (TP)parameter);
    }

    protected virtual T Convert(TV value, TP parameter)
    {
        throw new NotImplementedException(GetType().Name + "Convert not implemented");
    }
    protected virtual TV ConvertBack(T value, TP parameter)
    {
        throw new NotImplementedException(GetType().Name + "ConvertBack not implemented");
    }
}

public abstract class GenericValueConverter<TV, T> : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //if (value.GetType() != typeof(V)) throw new ArgumentException(GetType().Name + ".Convert: value type not " + typeof(V).Name);
        //if (targetType != typeof(T)) throw new ArgumentException(GetType().Name + ".Convert: target type not " + typeof(T).Name);
        //if (parameter != null) throw new ArgumentException(GetType().Name + ".Convert: binding contains unexpected parameter");
        return Convert((TV)value);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        //if (value.GetType() != typeof(T)) throw new ArgumentException(GetType().Name + ".ConvertBack: value type not " + typeof(T).Name);
        //if (targetType != typeof(V)) throw new ArgumentException(GetType().Name + ".ConvertBack: target type not " + typeof(V).Name);
        //if (parameter != null) throw new ArgumentException(GetType().Name + ".Convert: binding contains unexpected parameter");
        return ConvertBack((T)value);
    }

    protected virtual T Convert(TV value)
    {
        throw new NotImplementedException(GetType().Name + "Convert not implemented");
    }
    protected virtual TV ConvertBack(T value)
    {
        throw new NotImplementedException(GetType().Name + "ConvertBack not implemented");
    }
}
