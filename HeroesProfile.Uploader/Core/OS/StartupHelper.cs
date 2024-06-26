namespace HeroesProfile.Uploader.Core.OS;

public interface IStartupHelper
{
    bool Add();
    bool IsStartupEnabled();
    bool Remove();
}