using TMPro;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    [Header("Paneles de Navegación")]
    public GameObject authPanel;      // Contenedor padre de Auth (que agrupa mainMenu, login y register)
    public GameObject homeMenuPanel;  // El nuevo Menú Principal tras autenticarse
    public GameObject shopPanel;          // Tienda (lo creas tu manual)
    public GameObject customizationPanel; // Personalizacion (lo creas tu manual)

    [Header("Sub-Paneles de Autenticación")]
    public GameObject mainMenuPanel;
    public GameObject loginPanel;
    public GameObject registerPanel;

    [Header("Referencias de Inputs y Textos")]
    public TMP_InputField loginUsername;
    public TMP_InputField loginPassword;
    public TMP_InputField registerUsername;
    public TMP_InputField registerPassword;
    public TMP_Text statusText;
    public TMP_Text playerNameText;    // Texto en HomeMenuPanel donde se mostrará el nombre

    [Header("Monedas (arrastra los 3 pares: Home, Tienda, Personalizacion)")]
    public TMP_Text[] coinsTexts;
    public TMP_Text[] gemsTexts;

    [Header("Personalizacion de colores (todo con imagenes, sin texto)")]
    [Tooltip("El GameObject que tiene el script ColorWheel (arrastra el mismo, no la Image).")]
    public ColorWheel colorWheel;

    [Header("Previews (Image con color solido)")]
    [Tooltip("Image que muestra TU color.")]
    public UnityEngine.UI.Image playerColorPreview;
    [Tooltip("Image que muestra el color del ENEMIGO (siempre opuesto al tuyo).")]
    public UnityEngine.UI.Image enemyColorPreview;

    [Header("Botones de color (cada uno con el script ColorSwatch)")]
    [Tooltip("Cada uno es un GameObject con Button + ColorSwatch. El seleccionado se agranda.")]
    public ColorSwatch[] playerColorSwatches;

    private void Start()
    {
        if (authPanel == null) Debug.LogError("UIManager: authPanel NO asignado en el Inspector.");
        if (homeMenuPanel == null) Debug.LogError("UIManager: homeMenuPanel NO asignado en el Inspector.");
        if (mainMenuPanel == null || loginPanel == null || registerPanel == null)
        {
            Debug.LogError("UIManager: Paneles de UI no asignados en el Inspector.");
        }

        ShowAuthScreen();
        ShowMainMenu();

        // Si el auto-login ya termino antes de que la UI existiera, entra directo al Home.
        if (UGSAuthManager.Instance != null && UGSAuthManager.Instance.IsSignedIn)
        {
            _ = FetchConfigAndEnterHome();
        }
    }

    private void OnEnable()
    {
        UGSAuthManager.AutoLoginSucceeded += HandleAutoLogin;
    }

    private void OnDisable()
    {
        UGSAuthManager.AutoLoginSucceeded -= HandleAutoLogin;
    }

    private void HandleAutoLogin()
    {
        _ = FetchConfigAndEnterHome();
    }

    // --- NAVEGACIÓN ---
    public void ShowAuthScreen()
    {
        if (authPanel != null) authPanel.SetActive(true);
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
    }

    public void ShowHomeMenu()
    {
        Debug.Log("[UI] Mostrando HomeMenuPanel.");
        if (authPanel != null) authPanel.SetActive(false);
        else Debug.LogWarning("[UI] authPanel es null, no se puede ocultar.");
        if (homeMenuPanel != null) homeMenuPanel.SetActive(true);
        else Debug.LogWarning("[UI] homeMenuPanel es null, no se puede mostrar.");
        if (shopPanel != null) shopPanel.SetActive(false);
        if (customizationPanel != null) customizationPanel.SetActive(false);

        // Actualizar el nombre del jugador usando la nueva función de UGSAuthManager
        if (playerNameText != null && UGSAuthManager.Instance != null)
        {
            playerNameText.text = "Jugador: " + UGSAuthManager.Instance.GetPlayerName();
        }

        _ = RefreshCurrencyTexts();
    }

    /// <summary>Tienda: apaga Home y prende Shop. Conecta tu boton Tienda a este metodo.</summary>
    public void ShowShop()
    {
        Debug.Log("[UI] Mostrando ShopPanel.");
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
        if (customizationPanel != null) customizationPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(true);
        else Debug.LogWarning("[UI] shopPanel es null, asignalo en el Inspector.");

        _ = RefreshCurrencyTexts();
    }

    /// <summary>Personalizacion: apaga Home y prende Custom. Conecta tu boton a este metodo.</summary>
    public void ShowCustomization()
    {
        Debug.Log("[UI] Mostrando CustomizationPanel.");
        if (homeMenuPanel != null) homeMenuPanel.SetActive(false);
        if (shopPanel != null) shopPanel.SetActive(false);
        if (customizationPanel != null) customizationPanel.SetActive(true);
        else Debug.LogWarning("[UI] customizationPanel es null, asignalo en el Inspector.");

        _ = RefreshCurrencyTexts();
        RefreshColorPreviews();
    }

    /// <summary>Lee balances de la nube y los escribe en los 3 pares de textos.</summary>
    public async System.Threading.Tasks.Task RefreshCurrencyTexts()
    {
        await UGSEconomy.RefreshAsync();
        if (coinsTexts != null)
            foreach (var t in coinsTexts)
                if (t != null) t.text = UGSEconomy.Coins.ToString();
        if (gemsTexts != null)
            foreach (var t in gemsTexts)
                if (t != null) t.text = UGSEconomy.Gems.ToString();
    }

    public void ShowMainMenu()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowLoginPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(true);
        if (registerPanel != null) registerPanel.SetActive(false);
    }

    public void ShowRegisterPanel()
    {
        if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
        if (loginPanel != null) loginPanel.SetActive(false);
        if (registerPanel != null) registerPanel.SetActive(true);
    }

    // --- Personalizacion: color de tu ficha (el enemigo sale opuesto) ---
    // Rosa = 0, Naranja = 1, Personalizado = 2 (rueda). Conecta tus botones a estos.

    public void OnClickPlayerColorMode(int mode)
    {
        if (UGSProfileManager.Instance == null) return;
        if (playerColorSwatches == null || mode < 0 || mode >= playerColorSwatches.Length) return;
        var sw = playerColorSwatches[mode];
        if (sw == null) return;
        // El color sale del propio boton (cristal + liquido ya armados).
        _ = UGSProfileManager.Instance.SetCustomColorAsync(sw.Color);
        RefreshColorPreviews();
    }

    /// <summary>Lo llama la rueda al arrastrar. Activa modo Personalizado.</summary>
    public void OnWheelColorChanged(Color c)
    {
        if (UGSProfileManager.Instance == null) return;
        _ = UGSProfileManager.Instance.SetCustomColorAsync(c);
        RefreshColorPreviews();
    }

    /// <summary>Pinta los previews y marca el boton de color seleccionado.</summary>
    public void RefreshColorPreviews()
    {
        if (UGSProfileManager.Instance == null) return;

        if (playerColorPreview != null)
        {
            playerColorPreview.color = BoardFx.ColP1;
            playerColorPreview.raycastTarget = false;
        }
        if (enemyColorPreview != null)
        {
            enemyColorPreview.color = BoardFx.ColP2;
            enemyColorPreview.raycastTarget = false;
        }

        UpdateSwatchSelection();
    }

    // El seleccionado se agranda; los demas vuelven a escala normal.
    // Opcional: si el boton tiene un hijo llamado "SelectedFrame", se enciende.
    private void UpdateSwatchSelection()
    {
        if (playerColorSwatches == null) return;
        Color current = BoardFx.ColP1;
        for (int i = 0; i < playerColorSwatches.Length; i++)
        {
            var sw = playerColorSwatches[i];
            if (sw == null) continue;
            bool selected = ColorCloseEnough(sw.Color, current);
            sw.transform.localScale = selected ? Vector3.one * 1.18f : Vector3.one;

            var frame = sw.transform.Find("SelectedFrame");
            if (frame != null) frame.gameObject.SetActive(selected);
        }
    }

    private static bool ColorCloseEnough(Color a, Color b)
        => Mathf.Abs(a.r - b.r) < 0.02f && Mathf.Abs(a.g - b.g) < 0.02f && Mathf.Abs(a.b - b.b) < 0.02f;

    // --- ACCIONES DE AUTENTICACIÓN Y TRANSICIÓN AL HOME ---
    public async void OnClickGuest()
    {
        SetStatus("Conectando como invitado...");

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        try
        {
            bool success = await UGSAuthManager.Instance.SwitchToAnonymousAsync();
            if (success)
            {
                SetStatus("Cargando configuración...");
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error en login invitado.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickGuest: {ex}");
            SetStatus("Error al conectar como invitado.");
        }
    }

    public async void OnClickSubmitLogin()
    {
        if (loginUsername == null || loginPassword == null)
        {
            SetStatus("Campos de login no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(loginUsername.text) || string.IsNullOrEmpty(loginPassword.text))
        {
            SetStatus("Por favor ingresa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Iniciando sesión...");
        try
        {
            bool success = await UGSAuthManager.Instance.SwitchToUsernamePasswordAsync(loginUsername.text, loginPassword.text);
            if (success)
            {
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error de credenciales.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickSubmitLogin: {ex}");
            SetStatus("Error al iniciar sesión.");
        }
    }

    public async void OnClickSubmitRegister()
    {
        if (registerUsername == null || registerPassword == null)
        {
            SetStatus("Campos de registro no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(registerUsername.text) || string.IsNullOrEmpty(registerPassword.text))
        {
            SetStatus("Por favor completa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Creando cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.SignUpWithUsernamePasswordAsync(registerUsername.text, registerPassword.text);
            if (success)
            {
                await FetchConfigAndEnterHome();
            }
            else
            {
                SetStatus("Error al crear cuenta.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickSubmitRegister: {ex}");
            SetStatus("Error al crear cuenta.");
        }
    }

    public async void OnClickLinkAccount()
    {
        if (registerUsername == null || registerPassword == null)
        {
            SetStatus("Campos de registro no asignados.");
            return;
        }

        if (string.IsNullOrEmpty(registerUsername.text) || string.IsNullOrEmpty(registerPassword.text))
        {
            SetStatus("Por favor completa usuario y contraseña.");
            return;
        }

        if (UGSAuthManager.Instance == null)
        {
            SetStatus("Error: autenticador no inicializado.");
            return;
        }

        SetStatus("Vinculando cuenta...");
        try
        {
            bool success = await UGSAuthManager.Instance.LinkAnonymousToUsernamePasswordAsync(registerUsername.text, registerPassword.text);
            if (success)
            {
                SetStatus("¡Cuenta unificada con éxito!");
                ShowHomeMenu();
            }
            else
            {
                SetStatus("Error al vincular cuenta.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Excepción en OnClickLinkAccount: {ex}");
            SetStatus("Error al vincular cuenta.");
        }
    }

    public void OnClickLogout()
    {
        Debug.Log("[UI] Click Cerrar Sesion.");
        if (UGSAuthManager.Instance != null)
        {
            UGSAuthManager.Instance.SignOut();
        }
        else
        {
            Debug.LogWarning("[UI] UGSAuthManager.Instance es null en Logout.");
        }
        if (loginPassword != null) loginPassword.text = "";
        if (registerPassword != null) registerPassword.text = "";
        ShowAuthScreen();
        ShowMainMenu();
        SetStatus("Sesión cerrada.");
    }

    // --- MÉTODOS AUXILIARES ---
    private async System.Threading.Tasks.Task FetchConfigAndEnterHome()
    {
        await UGSRemoteConfig.FetchAsync();
        if (UGSProfileManager.Instance != null)
        {
            await UGSProfileManager.Instance.LoadProfileAsync();
        }
        ShowHomeMenu();
    }

    private void SetStatus(string message)
    {
        if (statusText != null) statusText.text = message;
    }
}
