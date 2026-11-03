using SpotifyAPI.Web;

namespace BotServer.Integrations.Spotify
{
    public class SpotyClient
    {
        private readonly SpotifyClient _client;

        public SpotyClient()
        {
            _client = new SpotifyClient("");
        }

        public async Task GetSong()
        {
            var s = new SearchRequest(SearchRequest.Types.Track, "another galaxy kremik");
            var result = await _client.Search.Item(s);

            if (result != null)
            {
                var first = result.Tracks.Items.FirstOrDefault();
                if (first == null) return;
                var songid = first.Id;

                var req = new PlayerResumePlaybackRequest { Uris = new List<string> { first.Uri } };

                await _client.Player.ResumePlayback(req);
            }
        }
    }
}
