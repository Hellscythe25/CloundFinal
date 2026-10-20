using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Authentication;

/// <summary>
/// Monedas del jugador SIN MonoBehaviour (clase estatica pura).
/// Uso: await UGSEconomy.RefreshAsync(); long gems = UGSEconomy.Gems;
///
/// Economy cerro para proyectos nuevos (sep-2026), asi que los balances viven
/// en Cloud Save dentro del perfil y los otorga Cloud Code tras validar cada
/// partida. La API publica no cambio: UIManager sigue igual.
/// </summary>
public static class UGSEconomy
{
    public static long Gems { get; private set; }
    public static long Coins { get; private set; }

    public static event System.Action BalancesUpdated;

    public static async Task RefreshAsync()
    {
        if (AuthenticationService.Instance == null || !AuthenticationService.Instance.IsSignedIn)
        {
            Debug.LogWarning("[Economy] Se requiere sesion para leer balances.");
            return;
        }

        if (UGSProfileManager.Instance == null)
        {
            Debug.LogError("[Economy] UGSProfileManager no disponible.");
            return;
        }

        // El perfil ya se cargo al entrar al Home; si viene vacio, se recarga.
        var p = UGSProfileManager.Instance.Profile;
        if (string.IsNullOrEmpty(p.PlayerName) && p.TotalMatches == 0 && p.Coins == 0 && p.Gems == 0)
            await UGSProfileManager.Instance.LoadProfileAsync();

        Gems = UGSProfileManager.Instance.Profile.Gems;
        Coins = UGSProfileManager.Instance.Profile.Coins;

        Debug.Log($"[Economy] GEMS={Gems} COINS={Coins} (Cloud Save)");
        BalancesUpdated?.Invoke();
        await Task.CompletedTask;
    }
}