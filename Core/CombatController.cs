using BotLockerApi;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DemoReplayFusion.Data;

namespace DemoReplayFusion.Core
{
    public class CombatController
    {
        private HashSet<CCSPlayerController> _released = new();
        private bool _bomb = false;
        private DateTime _roundStartTime;
        private bool _timerStarted = false;
        public bool IsBombPlanted => _bomb;

        public void StartTimer()
        {
            _roundStartTime = DateTime.Now;
            _timerStarted = true;
        }

        public bool CheckAndRelease(CCSPlayerController bot, DemoPlayer demo, DemoPlayer? next)
        {
            //an alternative time that allow you to customize the release time
            if (_timerStarted && (DateTime.Now - _roundStartTime).TotalSeconds >= 70)//here
            { Release(bot); return true; }

            if (_bomb) { Release(bot); return true; }
            if (demo.IsPlanting) { _bomb = true; ReleaseAll(); return true; }
            if (_released.Contains(bot)) { if (next != null && next.IsAlive) { _released.Remove(bot); ReleaseKeys(bot); return false; } return true; }
            if (next == null || !next.IsAlive) { Release(bot); return true; }
            if (IsEnemyInSight(bot)) { Release(bot); return true; }
            return false;
        }

        private bool IsEnemyInSight(CCSPlayerController bot)//it is not a good idea to use this function,may be you can mix with ray-trace
        {
            var p = bot.PlayerPawn?.Value; if (p == null) return false;
            var bp = p.CBodyComponent?.SceneNode?.AbsOrigin; if (bp == null) return false;
            int team = bot.TeamNum;
            foreach (var pl in Utilities.GetPlayers())
            {
                if (pl == bot || pl.TeamNum == team || !pl.PlayerPawn.IsValid) continue;
                var ep = pl.PlayerPawn.Value?.CBodyComponent?.SceneNode?.AbsOrigin; if (ep == null) continue;
                float dx = ep.X - bp.X, dy = ep.Y - bp.Y, dz = ep.Z - bp.Z, dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
                if (dist > 2000f) continue;
                var dir = new Vector(dx / dist, dy / dist, dz / dist);
                float ya = p.EyeAngles.Y * MathF.PI / 180f, pi = p.EyeAngles.X * MathF.PI / 180f;
                var fwd = new Vector(MathF.Cos(pi) * MathF.Cos(ya), MathF.Cos(pi) * MathF.Sin(ya), -MathF.Sin(pi));
                if (dir.X * fwd.X + dir.Y * fwd.Y + dir.Z * fwd.Z > 0.5f) return true;
            }
            return false;
        }

        private void Release(CCSPlayerController b) { _released.Add(b); ReleaseKeys(b); }
        private void ReleaseAll() { foreach (var b in Utilities.GetPlayers().Where(p => p.IsBot)) ReleaseKeys(b); _released.Clear(); }
        private void ReleaseKeys(CCSPlayerController b) { b.ExecuteClientCommand("-forward"); b.ExecuteClientCommand("-back"); b.ExecuteClientCommand("-left"); b.ExecuteClientCommand("-right"); b.ExecuteClientCommand("-duck"); BotLocker.Unlock((int)b.Index,LockKind.Weapon); BotLocker.Unlock((int)b.Index, LockKind.Aim); 
            }
        
        public void Reset() { _released.Clear(); _bomb = false; _timerStarted = false; }

        
    }
}