using CounterStrikeSharp.API.Core;
using DemoReplayFusion.Data;

namespace DemoReplayFusion.Core
{
    public class EconomyController
    {
        private HashSet<string> _synced = new();
        private static readonly Dictionary<string, int> Prices = new() { ["Flashbang"] = 200, ["Smoke Grenade"] = 300, ["HE Grenade"] = 300, ["Incendiary Grenade"] = 600, ["Molotov"] = 400, ["Decoy Grenade"] = 50 };
        private static readonly Dictionary<string, string> Cmds = new() { ["Flashbang"] = "give weapon_flashbang", ["Smoke Grenade"] = "give weapon_smokegrenade", ["HE Grenade"] = "give weapon_hegrenade", ["Incendiary Grenade"] = "give weapon_incgrenade", ["Molotov"] = "give weapon_molotov", ["Decoy Grenade"] = "give weapon_decoy" };

        public void SyncGrenades(CCSPlayerController bot, DemoPlayer demo)
        {
            if (demo?.Grenades == null) return;
            if (_synced.Contains(demo.SteamId)) return;
            int cost = 0;
            foreach (var g in demo.Grenades)
            { if (!string.IsNullOrEmpty(g) && Cmds.TryGetValue(g, out var cmd)) { bot.ExecuteClientCommand(cmd); cost += Prices.GetValueOrDefault(g, 0); } }
            if (cost > 0 && bot.InGameMoneyServices != null) bot.InGameMoneyServices.Account = Math.Max(0, bot.InGameMoneyServices.Account - cost);
            _synced.Add(demo.SteamId);
        }
        public void Reset() => _synced.Clear();
    }
}//this part is not function well,you can develop it