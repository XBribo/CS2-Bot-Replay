using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;
using DemoReplayFusion.Data;
using BotLockerApi;

namespace DemoReplayFusion.Core
{
    public class ReplayController
    {
        private Dictionary<string, float[]> _prevPositions = new();
        private Dictionary<string, string> _prevWeapons = new();
        private Dictionary<string, List<string>> _prevGrenades = new();
        private String? _lastMoveCommand = null;
        private DateTime _roundStartTime;
        private bool _timerStarted = false;
        private const double VIEW_TELEPORT_DURATION = 60.0;


        public void StartTimer()
        {
            _roundStartTime = DateTime.Now;
            _timerStarted = true;
        }

        public void Execute(CCSPlayerController bot, DemoPlayer demoPlayer)
        {
            var pawn = bot.PlayerPawn?.Value;
            if (pawn == null) return;
            if (demoPlayer.Origin == null || demoPlayer.ViewAngle == null) return;

            double elapsedSeconds = (DateTime.Now - _roundStartTime).TotalSeconds;

            // a math principle to caculate displacement
            float vx = 0f, vy = 0f, vz = 0f;
            string id = demoPlayer.SteamId;
            if (_prevPositions.TryGetValue(id, out float[] prev))
            {
                float dx = demoPlayer.Origin[0] - prev[0];
                float dy = demoPlayer.Origin[1] - prev[1];
                float dz = demoPlayer.Origin[2] - prev[2];
                float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

                if (dist > 0f)
                {
                    
                    float speed = dist * 64f;
                    vx = (dx / dist) * speed;
                    vy = (dy / dist) * speed;
                    vz = (dz / dist) * speed;
                }
            }

            if (elapsedSeconds >= 0)
            {
                pawn.Teleport(
                    new Vector(demoPlayer.Origin[0], demoPlayer.Origin[1], demoPlayer.Origin[2]),
                    null,
                    null);
            }
            if (vx != 0f || vy != 0f || vz != 0f)
            {
                pawn.AbsVelocity.X += vx;
                pawn.AbsVelocity.Y += vy;
                pawn.AbsVelocity.Z += vz;
            }


            if (_timerStarted && (DateTime.Now - _roundStartTime).TotalSeconds < VIEW_TELEPORT_DURATION)
            {
                pawn.Teleport(
                    null,
                    new QAngle(demoPlayer.ViewAngle[0], demoPlayer.ViewAngle[1], demoPlayer.ViewAngle[2]),
                    null);
            }

           

            //  duck
            LockWeapon(bot, demoPlayer);
            ApplyDuck(bot, demoPlayer);

            SaveState(demoPlayer);
        }

        //not avalible


        private void LockWeapon(CCSPlayerController bot, DemoPlayer demoPlayer)
        {
            string weaponName = demoPlayer.WeaponName ?? "";

            
            if (!string.IsNullOrEmpty(weaponName) && IsGrenade(weaponName))
            {
                BotLocker.Lock((int)bot.Index, LockTarget.Slot1);
                return;
            }
            //here using weapon lock
            if (string.IsNullOrEmpty(weaponName)) return;

            LockTarget target = weaponName.ToLower() switch
            {
                string w when w.Contains("knife") || w.Contains("zeus") => LockTarget.Slot3,
                string w when w.Contains("c4") => LockTarget.Slot5,
                string w when IsPistol(w) => LockTarget.Slot2,
                _ => LockTarget.Slot1
            };

            BotLocker.Lock((int)bot.Index, target);
        }


        private bool IsPistol(string w)
        {
            w = w.ToLower();
            return w.Contains("glock") || w.Contains("usp") || w.Contains("p250") || w.Contains("deagle")
                || w.Contains("tec") || w.Contains("five") || w.Contains("cz75") || w.Contains("elite")
                || w.Contains("p2000") || w.Contains("revolver") || w.Contains("hkp2000");
        }

        private bool IsGrenade(string w)
        {
            w = w.ToLower();
            return w.Contains("flash") || w.Contains("smoke") || w.Contains("hegrenade")
                || w.Contains("molotov") || w.Contains("incendiary") || w.Contains("decoy")
                || w.Contains("grenade");
        }


        private void ApplyDuck(CCSPlayerController bot, DemoPlayer demoPlayer)
        {
            var pawn = bot.PlayerPawn?.Value;
            if (pawn == null) return;

            if (demoPlayer.IsDucking)
            {
                
                pawn.AcceptInput("Crouch", pawn, pawn, "", 0);
            }
            else
            {
                
                pawn.AcceptInput("Stand", pawn, pawn, "", 0);
            }
        }


        private void SaveState(DemoPlayer p)
        {
            string id = p.SteamId;
            _prevPositions[id] = (float[])p.Origin.Clone();
            _prevWeapons[id] = p.WeaponName ?? "";
            _prevGrenades[id] = p.Grenades != null ? new List<string>(p.Grenades) : new List<string>();
        }

        public void Reset()
        {
            _prevPositions.Clear();
            _prevWeapons.Clear();
            _prevGrenades.Clear();
            _lastMoveCommand = null;
        }
    }
}