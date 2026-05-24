using System.Text.Json;



// The method of loading json demo
namespace DemoReplayFusion.Data
{
    public class JsonDataLoader
    {
        public List<DemoTick> AllTicks { get; private set; } = new();
        private Dictionary<int, List<int>> _roundToIndices = new();
        private string _logPath = "";

        public bool Load(string fileName, string moduleDirectory)
        {
            try
            {
                _logPath = Path.Combine(moduleDirectory, "demo_replay.log");

                string fullPath = Path.Combine(moduleDirectory, fileName);
                Log($"读取: {fullPath}");
                Log($"文件大小: {new FileInfo(fullPath).Length / 1024 / 1024} MB");

                using var stream = File.OpenRead(fullPath);
                AllTicks = JsonSerializer.Deserialize<List<DemoTick>>(stream) ?? new();

                Log($"共 {AllTicks.Count} 帧");

                if (AllTicks.Count == 0)
                {
                    Log("没有帧数据");
                    return false;
                }

                _roundToIndices.Clear();
                for (int i = 0; i < AllTicks.Count; i++)
                {
                    int round = AllTicks[i].RoundNumber;
                    if (!_roundToIndices.ContainsKey(round))
                        _roundToIndices[round] = new List<int>();
                    _roundToIndices[round].Add(i);
                }

                Log($"分组: {_roundToIndices.Count} 回合");
                Log($"keys: {string.Join(", ", _roundToIndices.Keys)}");
                foreach (var kv in _roundToIndices)
                    Log($"  回合 {kv.Key}: {kv.Value.Count} 帧");

                Log("加载成功");
                return true;
            }
            catch (Exception ex)
            {
                Log($"异常: {ex.Message}");
                return false;
            }
        }

        private void Log(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss}] [JsonDataLoader] {msg}";
                File.AppendAllText(_logPath, line + Environment.NewLine);
                Console.WriteLine(line);
            }
            catch { }
        }

        public string GetRoundKeys() => string.Join(", ", _roundToIndices.Keys);

        public int GetRoundCount() => _roundToIndices.Count;

        public int GetTickCount(int roundIndex)
        {
            if (_roundToIndices.TryGetValue(roundIndex, out var list))
                return list.Count;
            if (_roundToIndices.TryGetValue(roundIndex + 1, out var list2))
                return list2.Count;
            return 0;
        }

        public List<DemoPlayer>? GetPlayers(int roundIndex, int tickInRound)
        {
            if (!_roundToIndices.TryGetValue(roundIndex, out var list))
            {
                if (!_roundToIndices.TryGetValue(roundIndex + 1, out list))
                    return null;
            }
            if (tickInRound < 0 || tickInRound >= list.Count) return null;
            return AllTicks[list[tickInRound]].Players;
        }

        public List<DemoPlayer>? GetFirstFramePlayers()
        {
            for (int roundNum = 0; roundNum < _roundToIndices.Count; roundNum++)
            {
                if (!_roundToIndices.TryGetValue(roundNum, out var list))
                {
                    if (!_roundToIndices.TryGetValue(roundNum + 1, out list))
                        continue;
                }
                for (int i = 0; i < list.Count; i++)
                {
                    var tick = AllTicks[list[i]];
                    if (tick.Players != null && tick.Players.Count > 0)
                        return tick.Players;
                }
            }
            return null;
        }
    }
}