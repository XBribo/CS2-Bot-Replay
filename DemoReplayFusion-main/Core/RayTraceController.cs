using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Modules.Utils;
using RayTraceAPI;

namespace DemoReplayFusion.Core;

public class RayTraceController
{
    internal static PluginCapability<CRayTraceInterface> RayTraceInterface { get; } = new("raytrace:craytraceinterface");

    
    public void LockAndFire(CCSPlayerController bot)
    {
        var rayTrace = RayTraceInterface.Get();
        if (rayTrace == null) return;

        var botPawn = bot.PlayerPawn?.Value;
        if (botPawn == null) return;
        Vector botOrigin = botPawn.AbsOrigin!;

        foreach (var player in Utilities.GetPlayers())
        {
            if (player == bot || player.IsBot || !player.IsValid || !player.PawnIsAlive) continue;

            var targetPawn = player.PlayerPawn?.Value;
            if (targetPawn == null) continue;
            Vector targetOrigin = targetPawn.AbsOrigin!;

            float dx = botOrigin.X - targetOrigin.X;
            float dy = botOrigin.Y - targetOrigin.Y;
            float dz = botOrigin.Z - targetOrigin.Z;
            float distance = MathF.Sqrt(dx * dx + dy * dy + dz * dz);
            if (distance > 5000f) continue;

            
            if (!rayTrace.TraceEndShape(botOrigin, targetOrigin, botPawn, new TraceOptions(), out _))
            {
                
                Vector dir = targetOrigin - botOrigin;
                QAngle targetAngle = new(
                    MathF.Asin(-dir.Z / dir.Length()) * 180f / MathF.PI,
                    MathF.Atan2(dir.Y, dir.X) * 180f / MathF.PI,
                    0);

                
                botPawn.Teleport(null, targetAngle, null);

                
                bot.ExecuteClientCommand("+attack");
                return;
            }
        }
    }
}
