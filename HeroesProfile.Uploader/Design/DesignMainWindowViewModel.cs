using System;
using System.Linq;
using HeroesProfile.Uploader.Core;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.ViewModels;

namespace HeroesProfile.Uploader.Design;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    private string[] MapNames = new string[]
    {
        "Alterac Pass",
        "Battlefield of Eternity",
        "Blackheart's Bay",
        "Braxis Holdout",
        "Cursed Hollow",
        "Dragon Shire",
        "Garden of Terror",
        "Hanamura",
        "Haunted Mines",
        "Infernal Shrines",
        "Lost Cavern",
        "Sky Temple",
        "Tomb of the Spider Queen",
        "Towers of Doom",
        "Volskaya Foundry",
        "Warhead Junction"
    };
    
    public DesignMainWindowViewModel() : base()
    {
        var statuses = Enum.GetValuesAsUnderlyingType<UploadStatus>().Cast<UploadStatus>().Except(new[] { UploadStatus.None }).ToArray();
        
        Enumerable.Range(0, 100)
            .Select(i => new StormReplayInfo($"2024-06-{i + 1} 18.55.44 {MapNames[i % MapNames.Length]}.StormReplay")
            {
                UploadStatus = Random.Shared.GetItems(statuses, 1)[0]
            })
            .ToList()
            .ForEach(Files.Add);

        Files.GroupBy(x => x.UploadStatus)
            .Select(x => new StormReplayProcessResult { Count = x.Count(), UploadStatus = x.Key })
            .ToList()
            .ForEach(ProcessResults.Add);
    }
}