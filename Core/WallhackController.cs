using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Modules.Utils;

namespace DemoReplayFusion.Core;

public class WallhackController
{
    public bool Enabled { get; set; } = true;
    public float MaxDistance { get; set; } = 5000f;

    public void LockAndFire(CCSPlayerController bot)
    {
        if (!Enabled) return;

        var botPawn = bot.PlayerPawn?.Value;
        if (botPawn == null) return;
        Vector botOrigin = botPawn.AbsOrigin!;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == bot || player.IsBot || !player.IsValid || !player.PawnIsAlive) continue;

            var targetPawn = player.PlayerPawn?.Value;
            if (targetPawn == null) continue;

            // head position
            Vector targetHead = new(
                targetPawn.AbsOrigin!.X,
                targetPawn.AbsOrigin!.Y,
                targetPawn.AbsOrigin!.Z + 64f
            );

            // distant 
            float dx = botOrigin.X - targetHead.X;
            float dy = botOrigin.Y - targetHead.Y;
            float dz = botOrigin.Z - targetHead.Z;
            if (MathF.Sqrt(dx * dx + dy * dy + dz * dz) > MaxDistance) continue;

            // aim
            Vector dir = targetHead - botOrigin;
            QAngle targetAngle = new(
                MathF.Asin(-dir.Z / dir.Length()) * 180f / MathF.PI,
                MathF.Atan2(dir.Y, dir.X) * 180f / MathF.PI,
                0);


            //botPawn.Teleport(null, targetAngle, null);
            bot.ExecuteClientCommand("+attack");
            return;
        }
    }

    public CCSPlayerController? FindClosestPlayer(CCSPlayerController bot)
    {
        var botPawn = bot.PlayerPawn?.Value;
        if (botPawn == null) return null;
        Vector botOrigin = botPawn.AbsOrigin!;
        int botTeam = bot.TeamNum;

        CCSPlayerController? closest = null;
        float closestDist = 3000f;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == bot || player.IsBot || !player.IsValid || !player.PawnIsAlive) continue;
            if (player.TeamNum == botTeam) continue; // 跳过队友

            var targetPawn = player.PlayerPawn?.Value;
            if (targetPawn == null) continue;
            Vector targetOrigin = targetPawn.AbsOrigin!;

            float dx = botOrigin.X - targetOrigin.X;
            float dy = botOrigin.Y - targetOrigin.Y;
            float dz = botOrigin.Z - targetOrigin.Z;
            float dist = MathF.Sqrt(dx * dx + dy * dy + dz * dz);

            if (dist < closestDist)
            {
                closestDist = dist;
                closest = player;
            }
        }

        return closest;
    }
}