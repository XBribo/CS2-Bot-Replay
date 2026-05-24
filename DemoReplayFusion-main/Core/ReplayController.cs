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
        private const double VIEW_TELEPORT_DURATION = 50.0;


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
            string? currentMoveCommand = null;
            string id = demoPlayer.SteamId;

            if (_prevPositions.TryGetValue(id, out float[] prev))
            {
                float dx = demoPlayer.Origin[0] - prev[0];
                float dy = demoPlayer.Origin[1] - prev[1];
                float dist = MathF.Sqrt(dx * dx + dy * dy);

                if (dist > 5f)
                {
                    float yaw = demoPlayer.ViewAngle[1] * MathF.PI / 180f;
                    float fx = MathF.Cos(yaw), fy = MathF.Sin(yaw);
                    float forwardDot = dx * fx + dy * fy;
                    float rightDot = dx * (-fy) + dy * fx;

                    if (Math.Abs(forwardDot) >= Math.Abs(rightDot))
                        currentMoveCommand = forwardDot > 0 ? "+forward" : "+back";
                    else
                        currentMoveCommand = rightDot > 0 ? "+right" : "+left";
                }
            }





            if (elapsedSeconds >= 13)
            {
                pawn.Teleport(
                    new Vector(demoPlayer.Origin[0], demoPlayer.Origin[1], demoPlayer.Origin[2]),
                    null,
                    null);
            }

            if (_timerStarted && (DateTime.Now - _roundStartTime).TotalSeconds < VIEW_TELEPORT_DURATION)
            {
                pawn.Teleport(
                    null,
                    new QAngle(demoPlayer.ViewAngle[0], demoPlayer.ViewAngle[1], demoPlayer.ViewAngle[2]),
                    null);
            }

            var botPos = pawn.CBodyComponent?.SceneNode?.AbsOrigin;
            if (botPos != null)
            {
                float err = MathF.Sqrt(
                    (botPos.X - demoPlayer.Origin[0]) * (botPos.X - demoPlayer.Origin[0]) +
                    (botPos.Y - demoPlayer.Origin[1]) * (botPos.Y - demoPlayer.Origin[1]));

                if (err > 0f)
                {
                    pawn.Teleport(
                        new Vector(demoPlayer.Origin[0], demoPlayer.Origin[1], demoPlayer.Origin[2]),
                        null,
                        null);
                }
            }

            //  duck
            LockWeapon(bot, demoPlayer);
            ApplyDuck(bot, demoPlayer);

            SaveState(demoPlayer);
        }

        //not avalible
        private string? GetMovementCommand(CCSPlayerController bot, DemoPlayer demoPlayer)
        {
            string id = demoPlayer.SteamId;

            if (!_prevPositions.ContainsKey(id)) return null;

            float[] prev = _prevPositions[id];
            float dx = demoPlayer.Origin[0] - prev[0];
            float dy = demoPlayer.Origin[1] - prev[1];
            float dist = MathF.Sqrt(dx * dx + dy * dy);

            if (dist < 5f) return null;

            float yaw = demoPlayer.ViewAngle[1] * MathF.PI / 180f;
            float fx = MathF.Cos(yaw), fy = MathF.Sin(yaw);
            float forwardDot = dx * fx + dy * fy;
            float rightDot = dx * (-fy) + dy * fx;

            float absFwd = Math.Abs(forwardDot);
            float absRight = Math.Abs(rightDot);

            if (absFwd >= absRight)
                return forwardDot > 0 ? "+forward" : "+back";
            else
                return rightDot > 0 ? "+moveright" : "+moveleft";
        }


       

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
                // Demo里在蹲 -> 发送 "Crouch" 指令
                pawn.AcceptInput("Crouch", pawn, pawn, "", 0);
            }
            else
            {
                // Demo里在站 -> 发送 "Stand" 指令强制站立
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