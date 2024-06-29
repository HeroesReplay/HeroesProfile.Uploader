namespace HeroesProfile.Uploader.Os;

public interface IStartupHelper
{
    bool Add();
    bool IsStartupEnabled();
    bool Remove();
}