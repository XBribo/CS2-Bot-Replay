using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using DemoReplayFusion.Data;

namespace DemoReplayFusion.Core
{
    public class BotAssigner
    {
        private Dictionary<CCSPlayerController, string> _botToSteamId = new();

        public IEnumerable<CCSPlayerController> GetAssignedBots() => _botToSteamId.Keys;

        public void AssignBots(List<DemoPlayer> firstFramePlayers)
        {
            _botToSteamId.Clear();
            Console.WriteLine("========== Bot 分配开始 ==========");

            //assign bots with demo
            var ctPlayers = firstFramePlayers.Where(p => p.Side == 3).ToList();
            var tPlayers = firstFramePlayers.Where(p => p.Side == 2).ToList();
            Console.WriteLine($"[BotAssigner] Demo CT: {ctPlayers.Count}, Demo T: {tPlayers.Count}");

            // assign with T or CT
            var allBots = Utilities.GetPlayers().Where(p => p.IsBot).ToList();
            var ctBots = allBots.Where(b => b.TeamNum == 3).ToList();
            var tBots = allBots.Where(b => b.TeamNum == 2).ToList();
            Console.WriteLine($"[BotAssigner] Bot CT: {ctBots.Count}, Bot T: {tBots.Count}");

            
            for (int i = 0; i < ctBots.Count && i < ctPlayers.Count; i++)
            {
                _botToSteamId[ctBots[i]] = ctPlayers[i].SteamId;
                string oldName = ctBots[i].PlayerName;
                string newName = string.IsNullOrEmpty(ctPlayers[i].Name) ? $"Bot_CT_{i}" : ctPlayers[i].Name;
                ctBots[i].PlayerName = newName;
                Console.WriteLine($"[BotAssigner] CT: {oldName} → {newName}");
            }

            
            for (int i = 0; i < tBots.Count && i < tPlayers.Count; i++)
            {
                _botToSteamId[tBots[i]] = tPlayers[i].SteamId;
                string oldName = tBots[i].PlayerName;
                string newName = string.IsNullOrEmpty(tPlayers[i].Name) ? $"Bot_T_{i}" : tPlayers[i].Name;
                tBots[i].PlayerName = newName;
                Console.WriteLine($"[BotAssigner] T: {oldName} → {newName}");
            }

            Console.WriteLine($"[BotAssigner] 分配完成，共 {_botToSteamId.Count} 个 Bot");
        }

        public DemoPlayer? GetPlayerForBot(CCSPlayerController bot, List<DemoPlayer> currentFramePlayers)
        {
            if (!_botToSteamId.TryGetValue(bot, out string steamId))
                return null;

            foreach (var p in currentFramePlayers)
            {
                if (p.SteamId == steamId)
                    return p;
            }
            return null;
        }
    }
}