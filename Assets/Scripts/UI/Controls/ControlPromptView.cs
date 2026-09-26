using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A button prompt (e.g. "[E] Redo") that shows the matching gamepad button icon (Xbox, PlayStation, Switch or generic)
/// instead of the authored keyboard and mouse icons whenever the player is using a gamepad.
/// </summary>
public class ControlPromptView : MonoBehaviour
{
    [Header("Action")]
    [Tooltip("Action shown on a gamepad, in the form \"MapName/ActionName\". Leave empty to always show the authored keyboard/mouse prompt.")]
    [SerializeField] private string _actionPath;

    [Header("Components")]
    [SerializeField] private GameObject _keyCap;
    [SerializeField] private TMP_Text _keyCapText;
    [SerializeField] private GameObject _mouseIcons;

    private const float GAMEPAD_LABEL_MIN_FONT_SIZE = 8f;
    private const float GLYPH_INSET = 1f;

    // Keyboard/mouse look, as authored in the prefab.
    private bool _keyCapActive;
    private bool _mouseIconsActive;
    private bool _keyCapImageEnabled;
    private string _keyCapDefaultText;
    private Color _defaultTextColor;
    private float _defaultFontSize;
    private bool _defaultAutoSizing;
    private float _defaultFontSizeMin;
    private float _defaultFontSizeMax;
    private TextWrappingModes _defaultWrappingMode;

    private Image _keyCapImage;
    private Image _glyphBackground;
    private Image _glyphSymbol;

    private bool _isDefaultCached;

    private void Awake()
    {
        CacheDefaults();
    }

    private void OnEnable()
    {
        InputDeviceMonitor.OnSchemeChanged += OnSchemeChanged;
        Refresh();
    }

    private void OnDisable()
    {
        InputDeviceMonitor.OnSchemeChanged -= OnSchemeChanged;
    }

    private void CacheDefaults()
    {
        if (_isDefaultCached)
        {
            return;
        }
        _isDefaultCached = true;

        _keyCapActive = _keyCap.activeSelf;
        _mouseIconsActive = _mouseIcons.activeSelf;
        _keyCapImage = _keyCap.GetComponent<Image>();
        _keyCapImageEnabled = _keyCapImage != null && _keyCapImage.enabled;
        _keyCapDefaultText = _keyCapText.text;
        _defaultTextColor = _keyCapText.color;
        _defaultFontSize = _keyCapText.fontSize;
        _defaultAutoSizing = _keyCapText.enableAutoSizing;
        _defaultFontSizeMin = _keyCapText.fontSizeMin;
        _defaultFontSizeMax = _keyCapText.fontSizeMax;
        _defaultWrappingMode = _keyCapText.textWrappingMode;
    }

    private void OnSchemeChanged(ControlScheme _)
    {
        Refresh();
    }

    private void Refresh()
    {
        CacheDefaults();

        GamepadGlyph? glyph = InputDeviceMonitor.IsGamepad ? ControlPromptLabels.GetGamepadGlyph(_actionPath) : null;
        if (glyph.HasValue)
        {
            ShowGamepadGlyph(glyph.Value);
        }
        else
        {
            ShowKeyboardAndMouse();
        }
    }

    private void ShowGamepadGlyph(GamepadGlyph glyph)
    {
        EnsureGlyphImages();

        _mouseIcons.SetActive(false);
        _keyCap.SetActive(true);

        // The icon replaces the authored key cap.
        if (_keyCapImage != null)
        {
            _keyCapImage.enabled = false;
        }

        _glyphBackground.sprite = GamepadGlyphSprites.GetShape(glyph.Shape);
        _glyphBackground.color = glyph.Fill;
        _glyphBackground.gameObject.SetActive(true);

        Sprite symbol = GamepadGlyphSprites.GetSymbol(glyph.Symbol);
        _glyphSymbol.sprite = symbol;
        _glyphSymbol.color = glyph.Foreground;
        _glyphSymbol.gameObject.SetActive(symbol != null);

        // Names like "Options" are wider than the icon, so shrink them to fit.
        _keyCapText.textWrappingMode = TextWrappingModes.NoWrap;
        _keyCapText.fontSizeMin = GAMEPAD_LABEL_MIN_FONT_SIZE;
        _keyCapText.fontSizeMax = _defaultFontSize;
        _keyCapText.enableAutoSizing = true;
        _keyCapText.color = glyph.Foreground;
        _keyCapText.text = glyph.Text;
    }

    private void ShowKeyboardAndMouse()
    {
        if (_glyphBackground != null)
        {
            _glyphBackground.gameObject.SetActive(false);
            _glyphSymbol.gameObject.SetActive(false);
        }

        if (_keyCapImage != null)
        {
            _keyCapImage.enabled = _keyCapImageEnabled;
        }

        _mouseIcons.SetActive(_mouseIconsActive);
        _keyCap.SetActive(_keyCapActive);

        _keyCapText.enableAutoSizing = _defaultAutoSizing;
        _keyCapText.fontSizeMin = _defaultFontSizeMin;
        _keyCapText.fontSizeMax = _defaultFontSizeMax;
        _keyCapText.textWrappingMode = _defaultWrappingMode;
        _keyCapText.color = _defaultTextColor;
        _keyCapText.text = _keyCapDefaultText;
    }

    // The icon is two stacked images behind the key cap's text: the button shape and, for PlayStation, its symbol.
    private void EnsureGlyphImages()
    {
        if (_glyphBackground != null)
        {
            return;
        }

        _glyphBackground = CreateGlyphImage("GamepadGlyph");
        _glyphSymbol = CreateGlyphImage("GamepadGlyphSymbol");

        // Draw behind the key cap's text.
        _glyphBackground.transform.SetSiblingIndex(0);
        _glyphSymbol.transform.SetSiblingIndex(1);
    }

    private Image CreateGlyphImage(string objectName)
    {
        GameObject glyphObject = new(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glyphObject.layer = _keyCap.layer;

        RectTransform rectTransform = (RectTransform)glyphObject.transform;
        rectTransform.SetParent(_keyCap.transform, worldPositionStays: false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = new Vector2(GLYPH_INSET, GLYPH_INSET);
        rectTransform.offsetMax = new Vector2(-GLYPH_INSET, -GLYPH_INSET);

        Image image = glyphObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.preserveAspect = true;
        return image;
    }
}
