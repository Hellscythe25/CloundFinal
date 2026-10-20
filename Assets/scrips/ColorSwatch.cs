using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Boton de color de Personalizacion: se ve IGUAL que una casilla del juego
/// (cristal encima, liquido lleno debajo, mismo tint y mismo alpha del vidrio).
///
/// Uso por boton:
///   1. GameObject -> UI -> Button, tamano cuadrado (ej 120x120).
///   2. Add Component -> ColorSwatch.
///   3. Arrastra Crystal Sprite (0tapa) y Liquid Sprite (liquido animacion_15).
///   4. Pon el Color del boton (rosa, naranja, ...).
///   5. Conecta el OnClic a UIManager.OnClickPlayerColorMode con su indice.
///
/// El cristal se dibuja con el alpha del PNG (el vidrio ya trae la transparencia
/// horneada) y el liquido con el boost de brillo del juego.
/// </summary>
public class ColorSwatch : MonoBehaviour
{
    [Header("Arte (arrastra una vez)")]
    [Tooltip("0tapa (el cristal, nunca se tinta).")]
    public Sprite crystalSprite;
    [Tooltip("liquido animacion_15 (el frame lleno del nivel 3).")]
    public Sprite liquidSprite;

    [Header("Color del boton")]
    public Color color = Color.white;

    [Header("Igual que en el juego")]
    [Tooltip("Fraccion del boton que ocupa el liquido.")]
    [Range(0.3f, 1f)] public float liquidFill = 0.9f;
    [Tooltip("Fraccion del boton que ocupa el cristal.")]
    [Range(0.3f, 1f)] public float crystalFill = 1f;
    [Tooltip("Alpha del vidrio. 1 = respeta el PNG (trae su transparencia).")]
    [Range(0f, 1f)] public float glassAlpha = 1f;

    public Image liquidImage;
    public Image crystalImage;

    public Color Color => color;

    /// <summary>Cambia el color y retinta el liquido.</summary>
    public void SetColor(Color c)
    {
        color = c;
        if (liquidImage != null) liquidImage.color = BoardFx.Boost(c);
    }

    private void Awake() => Build();

    private void Build()
    {
        var rt = GetComponent<RectTransform>();
        if (rt == null) return;

        // Liquido: abajo, teñido y con el boost del juego.
        if (liquidImage == null && liquidSprite != null)
        {
            var go = new GameObject("Liquid");
            go.transform.SetParent(rt, false);
            var lrt = go.AddComponent<RectTransform>();
            Center(lrt, liquidFill);
            liquidImage = go.AddComponent<Image>();
            liquidImage.sprite = liquidSprite;
            liquidImage.color = BoardFx.Boost(color);
            liquidImage.raycastTarget = false;
        }

        // Cristal: encima, con su propio alpha (nunca se tinta).
        if (crystalImage == null && crystalSprite != null)
        {
            var go = new GameObject("Crystal");
            go.transform.SetParent(rt, false);
            var crt = go.AddComponent<RectTransform>();
            Center(crt, crystalFill);
            crystalImage = go.AddComponent<Image>();
            crystalImage.sprite = crystalSprite;
            crystalImage.color = new Color(1f, 1f, 1f, glassAlpha);
            crystalImage.raycastTarget = false;
        }
    }

    private static void Center(RectTransform child, float frac)
    {
        child.anchorMin = new Vector2(0.5f, 0.5f);
        child.anchorMax = new Vector2(0.5f, 0.5f);
        child.pivot = new Vector2(0.5f, 0.5f);
        float w = child.parent != null ? ((RectTransform)child.parent).rect.width : 100f;
        float h = child.parent != null ? ((RectTransform)child.parent).rect.height : 100f;
        child.sizeDelta = new Vector2(w * frac, h * frac);
    }
}