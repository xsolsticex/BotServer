

using SpotifyAPI.Web;

namespace BotServer.SpotifyClientApi
{
    public class SpotyClient
    {
        SpotifyClient client;
        
        
        public SpotyClient()
        {
            client = new SpotifyClient("BQDUGYv5QG2tZrrjdgDdxS-yxQbnQysCiNjAfvJJfdtHtQulJDQBZSoUuIBbpRvhZYT6yvJf2IEV6h1G-UeVE02a3TcYCL1DCIyP7bElxGHHqmiRJ2RbziuL5_kt405mBhiZmhd6U5f5t1E0fzwLACNRh9Jic-Ubv6vepY4EH0ntLWrA3WdJkL6HNVzY9QnGBPz21w2TEpqhCBvfA-KCf0C-HaLP1Xvph4kKRtm1li_CHX9rGccsjg01JZwbTHchC5c0swsMRFCG2zSRbgpKxoMIIA6jGZMobZ0vuc-1gwwpq76g3lgeOZK-o_26TO_G2o6hw24ZpETtEGqmMCBAp4kqCMIXa6Q");



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
