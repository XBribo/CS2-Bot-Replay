using CounterStrikeSharp.API.Core;
using DemoReplayFusion.Data;

namespace DemoReplayFusion.Core
{
    public class GrenadeController
    {
        private Dictionary<string, List<string>> _prev = new();
        private Dictionary<string, float> _prevZ = new();
        private HashSet<CCSPlayerController> _pendingRelease = new(), _pendingJump = new();
        private static readonly Dictionary<string, string> Slots = new() { ["Flashbang"] = "slot7", ["Smoke Grenade"] = "slot8", ["HE Grenade"] = "slot6", ["Incendiary Grenade"] = "slot10", ["Molotov"] = "slot10", ["Decoy Grenade"] = "slot9" };

        public void CheckAndThrow(CCSPlayerController bot, DemoPlayer demo)
        {
            if (demo?.Grenades == null) return;
            string id = demo.SteamId; float z = demo.Origin[2];
            if (!_prev.ContainsKey(id)) { _prev[id] = new(demo.Grenades); _prevZ[id] = z; return; }
            var prevList = _prev[id]; var curr = demo.Grenades;
            if (curr.Count >= prevList.Count) { _prev[id] = new(curr); _prevZ[id] = z; return; }
            var thrown = prevList.Except(curr).ToList();
            bool jump = (z - _prevZ[id]) > 10f;
            foreach (var g in thrown)
            {
                if (Slots.TryGetValue(g, out var s)) bot.ExecuteClientCommand(s);
                if (jump) { bot.ExecuteClientCommand("+jump"); _pendingJump.Add(bot); }
                else { bot.ExecuteClientCommand("+attack"); _pendingRelease.Add(bot); }
            }
            _prev[id] = new(curr); _prevZ[id] = z;
        }

        public void ProcessPendingJump() { foreach (var b in _pendingJump) { b.ExecuteClientCommand("+attack"); _pendingRelease.Add(b); } _pendingJump.Clear(); }
        public void ReleasePending() { foreach (var b in _pendingRelease) { b.ExecuteClientCommand("-attack"); b.ExecuteClientCommand("-jump"); } _pendingRelease.Clear(); }
        public void Reset() { _prev.Clear(); _prevZ.Clear(); _pendingRelease.Clear(); _pendingJump.Clear(); }
    }
}//this part is not function well,you can develop it