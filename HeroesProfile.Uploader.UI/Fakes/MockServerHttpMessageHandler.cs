using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using HeroesProfile.Uploader.Models;

namespace HeroesProfile.Uploader.UI.Fakes;

public class MockServerHttpMessageHandler : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        return request.RequestUri switch {
            { AbsolutePath: "/replays/fingerprints" } => HandleFingerprints(request),
            { AbsolutePath: "/upload/heroesprofile/desktop" } => HandleUpload(request),
            { AbsolutePath: "/openApi/Replay/Parsed" } => HandleReplayParsed(request),
            _ => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound))
        };
    }

    private async Task<HttpResponseMessage> HandleFingerprints(HttpRequestMessage request)
    {
        var isDuplicate = Random.Shared.Next(0, 2) == 0;

        // Fake API delay
        await Task.Delay(TimeSpan.FromSeconds(1));

        if (isDuplicate) {
            var payload = await request.Content!.ReadAsStringAsync();
            var fingerprints = payload.Split("\n");
            var fingerprintsJson = JsonSerializer.Serialize(fingerprints);
            var responseContent = $"{{\"exists\": {fingerprintsJson}}}";

            var response = new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(responseContent, Encoding.UTF8, "application/json") };
            return response;
        }

        return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{ \"exists\": [] }", Encoding.UTF8, "application/json") };
    }

    private async Task<HttpResponseMessage> HandleUpload(HttpRequestMessage request)
    {
        // Fake API delay
        await Task.Delay(TimeSpan.FromSeconds(1));

        // Randomly select an upload status
        var option = Enum.GetValues<UploadStatus>().MinBy(x => Random.Shared.Next());

        var result = new UploadResult() { Status = option, ReplayId = Random.Shared.Next(1, 1000), Fingerprint = Guid.NewGuid(), };

        var response = new HttpResponseMessage(HttpStatusCode.OK) {
            Content = new StringContent(JsonSerializer.Serialize(result), Encoding.UTF8, "application/json")
        };

        return response;
    }

    private Task<HttpResponseMessage> HandleReplayParsed(HttpRequestMessage request)
    {
        var response = new HttpResponseMessage(HttpStatusCode.BadRequest) {
            Content = new StringContent("{\"status\":\"success\"}", Encoding.UTF8, "application/json")
        };

        return Task.FromResult(response);
    }
}