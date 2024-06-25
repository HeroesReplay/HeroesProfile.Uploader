using System.IO;

namespace Heroesprofile.Uploader.Windows.UIHelpers;

public class FilenameConverter : GenericValueConverter<string, string>
{
    protected override string Convert(string value)
    {
        return Path.GetFileName(value);
    }
}
