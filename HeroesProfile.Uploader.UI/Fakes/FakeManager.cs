using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DynamicData;
using HeroesProfile.Uploader.Models;
using HeroesProfile.Uploader.Services;

namespace HeroesProfile.Uploader.UI.Fakes;

public class FakeManager : IManager
{
    public ISourceCache<StormReplayInfo, string> Files { get; } = new SourceCache<StormReplayInfo, string>(x => x.FileName);

    public FakeManager()
    {
        StartAsync();
    }

    ~FakeManager()
    {
        Stop();
    }

    public bool PreMatchPage { get; set; }
    public bool PostMatchPage { get; set; }

    public async Task StartAsync(CancellationToken token = default)
    {
        string[] map = [
            "Hanamura",
            "Battlefield of Eternity",
            "Blackheart's Bay",
            "Braxis Holdout",
            "Cursed Hollow",
            "Dragon Shire",
        ];

        foreach (var x in Enumerable.Range(0, 100)) {
            foreach (var i in Files.Items.Where(i => i.UploadStatus == UploadStatus.InProgress)) {
                i.UploadStatus = UploadStatus.Success;
                Files.AddOrUpdate(i);
            }

            foreach (var i in Files.Items.Where(i => i.UploadStatus == UploadStatus.Pending)) {
                i.UploadStatus = UploadStatus.InProgress;
                Files.AddOrUpdate(i);
            }

            Files.AddOrUpdate(new StormReplayInfo() {
                UploadStatus = UploadStatus.Pending, Created = DateTime.Now, FilePath = $"{DateTime.Now:yyyy-MM-dd-HH-mm-ss} {map[x % map.Length]}.StormReplay",
            });

            await Task.Delay(2000, token);
        }
    }

    public void Stop()
    {
    }
}