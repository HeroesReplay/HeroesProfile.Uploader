using System;
using System.Linq;
using DynamicData;
using HeroesProfile.Uploader.Core.Enums;
using HeroesProfile.Uploader.Core.Services;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.ViewModels;

namespace HeroesProfile.Uploader.Helpers;

public class DesignMainWindowViewModel : MainWindowViewModel
{
    private readonly string[] _mapNames = [
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
    ];
    
    public DesignMainWindowViewModel(Manager manager) : base(manager)
    {
        var statuses = Enum.GetValuesAsUnderlyingType<UploadStatus>().Cast<UploadStatus>().Except(new[] { UploadStatus.None }).ToArray();

        var items = Enumerable.Range(0, 100)
            .Select(i => {

                var status = Random.Shared.GetItems(statuses, 1)[0];
                var created = DateTime.Now.AddDays(-i);
                var map = _mapNames[i % _mapNames.Length];
                
                return new StormReplayInfo {
                    FilePath = $"{created:yyyy-MM-dd HH.mm.ss} {map}.StormReplay", 
                    Created = created, 
                    UploadStatus = status
                };
                
            })
            .ToList();
            
        Files.AddRange(items);
        
        // Set all to 0
        foreach (var item in Enum.GetValues<UploadStatus>()) {
            if (item == UploadStatus.None) continue;
            ProcessResults.Add(new StormReplayProcessResult() { UploadStatus = item, Count = 0 });
        }
        
        foreach (var group in Files.GroupBy(x => x.UploadStatus).Select(x => new StormReplayProcessResult { Count = x.Count(), UploadStatus = x.Key })) {
            
            for(int i = 0; i < ProcessResults.Count; i++) {
                if (ProcessResults[i].UploadStatus == group.UploadStatus) {
                    // ProcessResults[i] = group;
                    break;
                }
            }
        }
    }
}