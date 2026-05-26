using CounterStrikeSharp.API.Core;

namespace DemoReplayFusion.Core
{
    public class AvatarController
    {
        private HashSet<CCSPlayerController> _set = new();
        public void SetAvatar(CCSPlayerController bot, string steamId)
        {
            if (_set.Contains(bot) || string.IsNullOrEmpty(steamId)) return;
            try { bot.ExecuteClientCommand($"cl_avatar https://steamcommunity.com/profiles/{steamId}/avatar"); _set.Add(bot); } catch { }
        }
        public void Reset() => _set.Clear();
    }
}//this part is not function well,you can develop it