using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.Cvars;
using CounterStrikeSharp.API.Modules.Events;
using DemoReplayFusion.Core;
using DemoReplayFusion.Data;

namespace DemoReplayFusion
{
    public class DemoReplayFusion : BasePlugin
    {
        public override string ModuleName => "Demo Replay Fusion";
        public override string ModuleVersion => "1.0.0";

        private JsonDataLoader _dataLoader = new();
        private BotAssigner _botAssigner = new();
        private ReplayController _replayController = new();
        private CombatController _combatController = new();
        private EconomyController _economyController = new();
        private GrenadeController _grenadeController = new();
        private AvatarController _avatarController = new();
        private RayTraceController _rayTraceController = new();
        private readonly WallhackController _wallhack = new();

        private int _roundIndex = 0;
        private int _tickIndex = 0;
        private bool _replaying = false;
        private bool _dataLoaded = false;
        private int _currentGameRound = 0;
        private int _onTickCount = 0;

        public override void Load(bool hotReload)
        {
            ConVar.Find("sv_cheats")?.SetValue(true);
            Server.ExecuteCommand("mp_freezetime 11");         // 购买时间15秒 [citation:1]
            Server.ExecuteCommand("sv_falldamage_scale 0"); // 关闭跌落伤害 [citation:3]
            RegisterListener<Listeners.OnTick>(OnTick);
            RegisterListener<Listeners.OnMapStart>(_ =>
            {
                _replaying = false;
                _currentGameRound = 0;
            });

            
            RegisterEventHandler<EventRoundStart>((@event, info) =>
            {
                if (!_dataLoaded) return HookResult.Continue;
                _currentGameRound++;
                Log($"回合 {_currentGameRound} 开始（含购买期），启动回放");
                StartReplayForCurrentRound();
                _combatController.StartTimer();
                _replayController.StartTimer();
                return HookResult.Continue;
            });

           
            RegisterEventHandler<EventRoundEnd>((@event, info) =>
            {
                Log($"游戏回合 {_currentGameRound} 结束，暂停回放");
                _replaying = false;
                return HookResult.Continue;
            });

            Log("插件已加载（事件驱动模式，含购买期）");
        }

        [ConsoleCommand("css_loaddemo", "加载Demo JSON并回放")]
        public void OnLoadDemo(CCSPlayerController? player, CommandInfo command)
        {
            if (player == null) return;
            string path = command.ArgByIndex(1);
            if (string.IsNullOrEmpty(path)) { player.PrintToChat("用法: !loaddemo <json文件名>"); return; }
            LoadAndStart(player, path);
        }

        private void LoadAndStart(CCSPlayerController player, string path)
        {
            player.PrintToChat("🔄 正在加载...");
            Log($"开始加载: {path}");
            if (!_dataLoader.Load(path, ModuleDirectory))
            {
                player.PrintToChat("❌ 加载失败");
                Log("加载失败");
                return;
            }
            player.PrintToChat($"✅ {_dataLoader.GetRoundCount()} 个回合");
            Log($"加载成功，{_dataLoader.GetRoundCount()} 回合，等待回合开始事件");

            _dataLoaded = true;
            _currentGameRound = 0;
            _roundIndex = 0;
            _tickIndex = 0;
            _replaying = false;

            var first = _dataLoader.GetFirstFramePlayers();
            if (first != null && first.Count > 0)
            {
                var bots = Utilities.GetPlayers().Where(p => p.IsBot).ToList();
                if (bots.Count >= first.Count)
                {
                    _botAssigner.AssignBots(first);
                    Log("初始 Bot 分配完成");
                }
            }

            _replayController.Reset();
            _combatController.Reset();
            _economyController.Reset();
            _grenadeController.Reset();
            _avatarController.Reset();

            AddTimer(1f, () => Server.ExecuteCommand("mp_restartgame 1"));
        }

        private void StartReplayForCurrentRound()
        {
            if (_dataLoader.AllTicks.Count == 0) return;

            for (int i = 0; i < _dataLoader.AllTicks.Count; i++)
            {
                if (_dataLoader.AllTicks[i].RoundNumber == _currentGameRound)
                {
                    _tickIndex = i;
                    _roundIndex = _currentGameRound;
                    _replaying = true;
                    _onTickCount = 0;

                    _replayController.Reset();
                    _combatController.Reset();
                    _economyController.Reset();
                    _grenadeController.Reset();
                    _avatarController.Reset();

                    var tick = _dataLoader.AllTicks[i];
                    if (tick.Players != null && tick.Players.Count > 0)
                        _botAssigner.AssignBots(tick.Players);

                    Log($"回放开始: 游戏回合 {_currentGameRound}, tickIndex={_tickIndex}");
                    return;
                }
            }

            Log($"警告: 未找到游戏回合 {_currentGameRound} 的 Demo 数据");
        }

        private void OnTick()
        {
            if (!_replaying || !_dataLoaded) return;

            _onTickCount++;

            if (_tickIndex >= _dataLoader.AllTicks.Count)
            {
                _replaying = false;
                Log($"tickIndex 超出范围");
                return;
            }

            var currentTick = _dataLoader.AllTicks[_tickIndex];

            if (currentTick.RoundNumber != _currentGameRound)
            {
                _replaying = false;
                Log($"回合 {_currentGameRound} 数据播放完毕，共 {_onTickCount} 帧");
                return;
            }

            var nextTick = (_tickIndex + 1 < _dataLoader.AllTicks.Count) ? _dataLoader.AllTicks[_tickIndex + 1] : null;
            var cur = currentTick.Players;
            var next = nextTick?.Players;

            if (cur == null) return;

            int botProcessed = 0;
            int botReleased = 0;

            try
            {
                foreach (var b in _botAssigner.GetAssignedBots())
                {
                    if (b.PlayerPawn?.Value == null) continue;
                    var dp = _botAssigner.GetPlayerForBot(b, cur);
                    if (dp == null) continue;
                    var ndp = next != null ? _botAssigner.GetPlayerForBot(b, next) : null;
                    _economyController.SyncGrenades(b, dp);

                    if (!_combatController.CheckAndRelease(b, dp, ndp))
                    {
                        _grenadeController.CheckAndThrow(b, dp);
                        var target = _wallhack.FindClosestPlayer(b);
                        if (target != null)
                        {
                            
                            _wallhack.LockAndFire(b);
                            continue; 
                        }
                        _rayTraceController.LockAndFire(b);
                        _replayController.Execute(b, dp);
                        botProcessed++;
                    }
                    else botReleased++;
                }
                _grenadeController.ProcessPendingJump();
                _grenadeController.ReleasePending();
            }
            catch (Exception ex)
            {
                Log($"foreach 异常: {ex.GetType().Name} - {ex.Message}");
            }

            if (_onTickCount <= 3 || _onTickCount % 1000 == 0)
                Log($"OnTick #{_onTickCount}: tickIndex={_tickIndex}, 处理={botProcessed}, 释放={botReleased}");

            _tickIndex++;
        }


        [ConsoleCommand("css_stopdemo", "停止回放")]
        public void OnStopDemo(CCSPlayerController? player, CommandInfo command)
        {
            _replaying = false;
            _dataLoaded = false;
            _replayController.Reset(); _combatController.Reset(); _economyController.Reset(); _grenadeController.Reset(); _avatarController.Reset();
            player?.PrintToChat("回放已停止");
            Log("回放已停止");
        }

        private void Log(string msg)
        {
            try
            {
                string logPath = Path.Combine(ModuleDirectory, "demo_replay.log");
                string line = $"[{DateTime.Now:HH:mm:ss}] [DemoReplay] {msg}";
                File.AppendAllText(logPath, line + Environment.NewLine);
            }
            catch { }
        }
    }
}