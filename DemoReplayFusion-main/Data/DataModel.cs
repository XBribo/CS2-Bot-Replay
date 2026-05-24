using System.Text.Json.Serialization;

namespace DemoReplayFusion.Data
{
    public class DemoTick
    {
        [JsonPropertyName("frame")] public int Frame { get; set; }
        [JsonPropertyName("tick")] public int Tick { get; set; }
        [JsonPropertyName("roundNumber")] public int RoundNumber { get; set; }
        [JsonPropertyName("players")] public List<DemoPlayer> Players { get; set; } = new();
    }
    //a data struction of input demo
    public class DemoPlayer
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "";
        [JsonPropertyName("steamId")] public string SteamId { get; set; } = "";
        [JsonPropertyName("side")] public int Side { get; set; }
        [JsonPropertyName("isAlive")] public bool IsAlive { get; set; }
        [JsonPropertyName("health")] public int Health { get; set; }
        [JsonPropertyName("armor")] public int Armor { get; set; }
        [JsonPropertyName("hasHelmet")] public bool HasHelmet { get; set; }
        [JsonPropertyName("hasBomb")] public bool HasBomb { get; set; }
        [JsonPropertyName("hasDefuseKit")] public bool HasDefuseKit { get; set; }
        [JsonPropertyName("isDucking")] public bool IsDucking { get; set; }
        [JsonPropertyName("isPlanting")] public bool IsPlanting { get; set; }
        [JsonPropertyName("money")] public int Money { get; set; }
        [JsonPropertyName("origin")] public float[] Origin { get; set; } = new float[3];
        [JsonPropertyName("viewAngle")] public float[] ViewAngle { get; set; } = new float[3];
        [JsonPropertyName("weaponName")] public string WeaponName { get; set; } = "";
        [JsonPropertyName("equipments")] public List<string> Equipments { get; set; } = new();
        [JsonPropertyName("grenades")] public List<string> Grenades { get; set; } = new();
        [JsonPropertyName("pistols")] public List<string> Pistols { get; set; } = new();
        [JsonPropertyName("smgs")] public List<string> Smgs { get; set; } = new();
        [JsonPropertyName("rifles")] public List<string> Rifles { get; set; } = new();
        [JsonPropertyName("heavy")] public List<string> Heavy { get; set; } = new();
    }
}