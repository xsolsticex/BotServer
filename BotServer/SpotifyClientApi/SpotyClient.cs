

using SpotifyAPI.Web;

namespace BotServer.SpotifyClientApi
{
    public class SpotyClient
    {
        SpotifyClient client;
        
        
        public SpotyClient()
        {
            client = new SpotifyClient("");



        }



        public async Task GetSong()
        {
            SearchRequest s  = new SearchRequest(SearchRequest.Types.Track,"another galaxy kremik");
            var result = await client.Search.Item(s);
            
            if(result != null)
            {
                var first = result.Tracks.Items.FirstOrDefault();
                var songid = first.Id;

                PlayerResumePlaybackRequest req = new PlayerResumePlaybackRequest {Uris = new List<string> { first.Uri} };
    
                await client.Player.ResumePlayback(req);
            }
            //var song = await client.Search.
            
     

        }
    }
}
