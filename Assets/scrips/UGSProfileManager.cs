using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;

/// <summary>
/// Agente 3: Perfil + dificultad adaptativa SIN MonoBehaviour (clase pura).
/// Uso: await UGSProfileManager.Instance.LoadProfileAsync();
/// No requiere GameObject en escena.
/// </summary>
public class UGSProfileManager
{
    public static readonly UGSProfileManager Instance = new UGSProfileManager();

    [System.Serializable]
    public class PlayerProfile
    {
        public string PlayerName = "";
        public List<string> SkinsOwned = new List<string>();
        public string EquippedSkin = "default";
        public int TotalWins = 0;
        public int TotalMatches = 0;
        // Pack de animaciones equipado (lo cambia el jugador en su UI)
        public List<string> OwnedPackIds = new List<string> { "clasico" };
        public string EquippedPackId = "clasico";
        // Monedas virtuales (Economy esta cerrado para proyectos nuevos,
        // asi que viven en Cloud Save y las otorga Cloud Code).
        public long Coins = 0;
        public long Gems = 0;
        // Color de tu ficha (modo: 0 rosa, 1 naranja, 2 personalizado) y el color
        // custom de la rueda. El enemigo sale opuesto, no se guarda.
        public int PlayerColorMode = 0;
        public Color PlayerCustomColor = new Color(1f, 0.15f, 0.6f);
    }

    public PlayerProfile Profile = new PlayerProfile();

    private UGSProfileManager() { }

    public float WinRate => Profile.TotalMatches == 0 ? 0f : (float)Profile.TotalWins / Profile.TotalMatches;

    public BotAgent.Difficulty SuggestedDifficulty
    {
        get
        {
            if (Profile.TotalMatches < 3) return BotAgent.Difficulty.Medio;
            if (WinRate >= 0.65f) return BotAgent.Difficulty.Dificil;
            if (WinRate <= 0.35f) return BotAgent.Difficulty.Facil;
            return BotAgent.Difficulty.Medio;
        }
    }

    private bool CanUseCloudSave()
        => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn;

    public async Task LoadProfileAsync()
    {
        if (!CanUseCloudSave()) { Debug.LogWarning("[Profile] Sin sesion, perfil local."); return; }
        try
        {
            var keys = new HashSet<string> { "PlayerName", "SkinsOwned", "EquippedSkin", "TotalWins", "TotalMatches", "OwnedPacks", "EquippedPack", "Coins", "Gems", "PlayerColor", "EnemyColor" };
            var data = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);
            if (data.TryGetValue("PlayerName", out var n)) Profile.PlayerName = n.Value.GetAs<string>();
            if (data.TryGetValue("EquippedSkin", out var s)) Profile.EquippedSkin = s.Value.GetAs<string>();
            if (data.TryGetValue("TotalWins", out var w)) Profile.TotalWins = w.Value.GetAs<int>();
            if (data.TryGetValue("TotalMatches", out var m)) Profile.TotalMatches = m.Value.GetAs<int>();
            if (data.TryGetValue("Coins", out var c)) Profile.Coins = c.Value.GetAs<long>();
            if (data.TryGetValue("Gems", out var g)) Profile.Gems = g.Value.GetAs<long>();
            if (data.TryGetValue("PlayerColor", out var pc)) Profile.PlayerColorMode = pc.Value.GetAs<int>();
            if (data.TryGetValue("PlayerCustomColor", out var pcc))
            {
                string csv = pcc.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(csv))
                {
                    var parts = csv.Split(',');
                    if (parts.Length == 3 &&
                        int.TryParse(parts[0], out int rr) && int.TryParse(parts[1], out int gg) && int.TryParse(parts[2], out int bb))
                        Profile.PlayerCustomColor = new Color(rr / 255f, gg / 255f, bb / 255f);
                }
            }
            ApplyColors();
            if (data.TryGetValue("SkinsOwned", out var sk))
            {
                string skinsJson = sk.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(skinsJson))
                    Profile.SkinsOwned = JsonUtility.FromJson<SkinList>(skinsJson)?.skins ?? new List<string>();
            }
            if (data.TryGetValue("OwnedPacks", out var op))
            {
                string packsJson = op.Value.GetAs<string>();
                if (!string.IsNullOrEmpty(packsJson))
                    Profile.OwnedPackIds = JsonUtility.FromJson<SkinList>(packsJson)?.skins ?? new List<string>();
            }
            if (Profile.OwnedPackIds.Count == 0) Profile.OwnedPackIds = new List<string> { "clasico" };
            if (data.TryGetValue("EquippedPack", out var ep) && !string.IsNullOrEmpty(ep.Value.GetAs<string>()))
                Profile.EquippedPackId = ep.Value.GetAs<string>();
            Debug.Log($"[Profile] Cargado. WinRate={WinRate:P0} -> Bot sugerido={SuggestedDifficulty}");
        }
        catch (System.Exception e) { Debug.LogError($"[Profile] Error load: {e.Message}"); }
    }

    public async Task SaveProfileAsync()
    {
        if (!CanUseCloudSave()) return;
        try
        {
            var data = new Dictionary<string, object>
            {
                { "PlayerName", Profile.PlayerName },
                { "EquippedSkin", Profile.EquippedSkin },
                { "TotalWins", Profile.TotalWins },
                { "TotalMatches", Profile.TotalMatches },
                { "EquippedPack", Profile.EquippedPackId },
                { "Coins", Profile.Coins },
                { "Gems", Profile.Gems },
                { "PlayerColor", Profile.PlayerColorMode },
                { "PlayerCustomColor", $"{Mathf.RoundToInt(Profile.PlayerCustomColor.r * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor.g * 255)},{Mathf.RoundToInt(Profile.PlayerCustomColor.b * 255)}" },
                { "SkinsOwned", JsonUtility.ToJson(new SkinList { skins = Profile.SkinsOwned }) },
                { "OwnedPacks", JsonUtility.ToJson(new SkinList { skins = Profile.OwnedPackIds }) }
            };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            Debug.Log("[Profile] Guardado OK.");
        }
        catch (System.Exception e) { Debug.LogError($"[Profile] Error save: {e.Message}"); }
    }

    public async Task RecordMatchResultAsync(bool won)
    {
        Profile.TotalMatches++;
        if (won) Profile.TotalWins++;
        await SaveProfileAsync();
    }

    public bool OwnsPack(string packId) => Profile.OwnedPackIds.Contains(packId);

    // Otorga un pack (tu tienda lo llama al comprar). Guarda en la nube.
    public async Task<bool> UnlockPackAsync(string packId)
    {
        if (FxPackCatalog.Get(packId) == null || OwnsPack(packId)) return false;
        Profile.OwnedPackIds.Add(packId);
        await SaveProfileAsync();
        return true;
    }

    // Equipa el pack: desde aqui el tablero usa sus 4 animaciones. Guarda en la nube.
    public async Task<bool> EquipPackAsync(string packId)
    {
        if (FxPackCatalog.Get(packId) == null || !OwnsPack(packId)) return false;
        Profile.EquippedPackId = packId;
        await SaveProfileAsync();
        return true;
    }

    // Suma monedas (las otorga Cloud Code tras validar la partida). Guarda en la nube.
    public async Task GrantCurrencyAsync(long coins, long gems)
    {
        Profile.Coins = System.Math.Max(0, Profile.Coins + coins);
        Profile.Gems = System.Math.Max(0, Profile.Gems + gems);
        await SaveProfileAsync();
    }

    // Gasta monedas en la tienda. False si no alcanza. Guarda en la nube.
    public async Task<bool> SpendCurrencyAsync(long coins, long gems)
    {
        if (Profile.Coins < coins || Profile.Gems < gems) return false;
        Profile.Coins -= coins;
        Profile.Gems -= gems;
        await SaveProfileAsync();
        return true;
    }

    // --- Color de ficha (Personalizacion) ---
    /// <summary>Pinta el tablero con tu color. El enemigo sale opuesto.</summary>
    public void ApplyColors()
        => BoardFx.ApplyColors(Profile.PlayerColorMode, Profile.PlayerCustomColor);

    /// <summary>Elige el modo de color: 0 rosa, 1 naranja, 2 personalizado. Guarda.</summary>
    public async Task SetPlayerColorModeAsync(int mode)
    {
        Profile.PlayerColorMode = Mathf.Clamp(mode, 0, PlayerColorOptions.CustomIndex);
        ApplyColors();
        await SaveProfileAsync();
    }

    /// <summary>Color libre desde la rueda. Activa el modo Personalizado. Guarda.</summary>
    public async Task SetCustomColorAsync(Color c)
    {
        Profile.PlayerCustomColor = c;
        Profile.PlayerColorMode = PlayerColorOptions.CustomIndex;
        ApplyColors();
        await SaveProfileAsync();
    }

    [System.Serializable] private class SkinList { public List<string> skins; }
}
