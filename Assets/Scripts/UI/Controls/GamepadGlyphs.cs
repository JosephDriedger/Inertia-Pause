using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.LowLevel;

public enum GamepadFamily
{
    Generic,
    Xbox,
    PlayStation,
    Switch,
}

/// <summary>
/// The icon drawn on a symbol button, for PlayStation face buttons that have no letter.
/// </summary>
public enum GlyphSymbol
{
    None,
    Cross,
    Circle,
    Square,
    Triangle,
}

public enum GlyphShape
{
    Circle,
    RoundedSquare,
}

/// <summary>
/// How a gamepad button is shown to the player: a name for use in sentences and the look of its icon.
/// </summary>
public readonly struct GamepadGlyph
{
    /// <summary>
    /// The button's name as printed on the controller, for use in sentences (e.g. "A", "Cross", "RB").
    /// </summary>
    public readonly string Name;

    /// <summary>
    /// Text drawn on the icon, empty when the icon is a symbol.
    /// </summary>
    public readonly string Text;
    public readonly GlyphSymbol Symbol;
    public readonly GlyphShape Shape;
    public readonly Color Fill;

    /// <summary>
    /// Color of the icon's text or symbol.
    /// </summary>
    public readonly Color Foreground;

    public GamepadGlyph(string name, string text, GlyphSymbol symbol, GlyphShape shape, Color fill, Color foreground)
    {
        Name = name;
        Text = text;
        Symbol = symbol;
        Shape = shape;
        Fill = fill;
        Foreground = foreground;
    }
}

/// <summary>
/// Detects which kind of controller is connected and describes its buttons the way that controller labels them.
/// </summary>
public static class GamepadGlyphs
{
    private const string GAMEPAD_PATH_PREFIX = "<Gamepad>/";

    private const int MICROSOFT_VENDOR_ID = 0x045E;
    private const int SONY_VENDOR_ID = 0x054C;
    private const int NINTENDO_VENDOR_ID = 0x057E;

    private static readonly Color DARK_FILL = new(0.16f, 0.17f, 0.21f);
    private static readonly Color BUTTON_FILL = new(0.24f, 0.25f, 0.30f);
    private static readonly Color LIGHT_FILL = new(0.88f, 0.88f, 0.90f);
    private static readonly Color GENERIC_FILL = new(0.70f, 0.72f, 0.76f);
    private static readonly Color LIGHT_TEXT = Color.white;
    private static readonly Color DARK_TEXT = new(0.12f, 0.12f, 0.14f);

    // Xbox face buttons.
    private static readonly Color XBOX_GREEN = new(0.30f, 0.72f, 0.24f);
    private static readonly Color XBOX_RED = new(0.89f, 0.27f, 0.25f);
    private static readonly Color XBOX_BLUE = new(0.22f, 0.48f, 0.90f);
    private static readonly Color XBOX_YELLOW = new(0.98f, 0.73f, 0.12f);

    // PlayStation face button symbols.
    private static readonly Color PLAYSTATION_BLUE = new(0.42f, 0.62f, 0.95f);
    private static readonly Color PLAYSTATION_RED = new(0.95f, 0.36f, 0.38f);
    private static readonly Color PLAYSTATION_PINK = new(0.93f, 0.58f, 0.80f);
    private static readonly Color PLAYSTATION_GREEN = new(0.35f, 0.80f, 0.58f);

    /// <summary>
    /// Works out what kind of controller the gamepad is from its type, name and vendor.
    /// </summary>
    public static GamepadFamily GetFamily(Gamepad gamepad)
    {
        if (gamepad == null)
        {
            return GamepadFamily.Xbox;
        }

        // Some Xbox controllers only reach the game as a generic device that is mirrored onto a virtual gamepad.
        if (HidGamepadAdapter.IsVirtualGamepad(gamepad))
        {
            return GamepadFamily.Xbox;
        }

        InputDeviceDescription description = gamepad.description;
        string identity = $"{gamepad.layout} {description.product} {description.manufacturer}".ToLowerInvariant();
        int vendorId = GetHidVendorId(description);

        if (identity.Contains("dualshock") || identity.Contains("dualsense") || identity.Contains("playstation")
            || identity.Contains("sony") || vendorId == SONY_VENDOR_ID)
        {
            return GamepadFamily.PlayStation;
        }

        if (identity.Contains("switch") || identity.Contains("nintendo") || identity.Contains("joy-con")
            || identity.Contains("pro controller") || vendorId == NINTENDO_VENDOR_ID)
        {
            return GamepadFamily.Switch;
        }

        if (identity.Contains("xbox") || identity.Contains("xinput") || vendorId == MICROSOFT_VENDOR_ID)
        {
            return GamepadFamily.Xbox;
        }

        return GamepadFamily.Generic;
    }

    /// <summary>
    /// Describes the gamepad control at the path (e.g. "&lt;Gamepad&gt;/buttonSouth") for the controller in use.
    /// </summary>
    public static GamepadGlyph ForPath(string controlPath, GamepadFamily family)
    {
        string control = controlPath.StartsWith(GAMEPAD_PATH_PREFIX) ? controlPath.Substring(GAMEPAD_PATH_PREFIX.Length) : controlPath;

        switch (control)
        {
            case "buttonSouth":
                return FaceButton(family, GamepadButton.South);
            case "buttonEast":
                return FaceButton(family, GamepadButton.East);
            case "buttonWest":
                return FaceButton(family, GamepadButton.West);
            case "buttonNorth":
                return FaceButton(family, GamepadButton.North);
            case "leftShoulder":
                return Pill(family switch { GamepadFamily.PlayStation => "L1", GamepadFamily.Switch => "L", _ => "LB" });
            case "rightShoulder":
                return Pill(family switch { GamepadFamily.PlayStation => "R1", GamepadFamily.Switch => "R", _ => "RB" });
            case "leftTrigger":
                return Pill(family switch { GamepadFamily.PlayStation => "L2", GamepadFamily.Switch => "ZL", _ => "LT" });
            case "rightTrigger":
                return Pill(family switch { GamepadFamily.PlayStation => "R2", GamepadFamily.Switch => "ZR", _ => "RT" });
            case "start":
                return Pill(family switch { GamepadFamily.PlayStation => "Options", GamepadFamily.Switch => "+", GamepadFamily.Xbox => "Menu", _ => "Start" });
            case "select":
                return Pill(family switch { GamepadFamily.PlayStation => "Share", GamepadFamily.Switch => "-", GamepadFamily.Xbox => "View", _ => "Select" });
            case "leftStickPress":
                return Pill(family == GamepadFamily.Xbox ? "LS" : "L3");
            case "rightStickPress":
                return Pill(family == GamepadFamily.Xbox ? "RS" : "R3");
            case "leftStick":
                return Pill("L Stick");
            case "rightStick":
                return Pill("R Stick");
            default:
                // The D-pad and its directions.
                return control.StartsWith("dpad") ? Pill("D-Pad") : Pill(control);
        }
    }

    private static GamepadGlyph Pill(string text)
    {
        return new GamepadGlyph(text, text, GlyphSymbol.None, GlyphShape.RoundedSquare, BUTTON_FILL, LIGHT_TEXT);
    }

    private static GamepadGlyph FaceButton(GamepadFamily family, GamepadButton button)
    {
        switch (family)
        {
            case GamepadFamily.PlayStation:
                return button switch
                {
                    GamepadButton.South => PlayStationSymbol("Cross", GlyphSymbol.Cross, PLAYSTATION_BLUE),
                    GamepadButton.East => PlayStationSymbol("Circle", GlyphSymbol.Circle, PLAYSTATION_RED),
                    GamepadButton.West => PlayStationSymbol("Square", GlyphSymbol.Square, PLAYSTATION_PINK),
                    _ => PlayStationSymbol("Triangle", GlyphSymbol.Triangle, PLAYSTATION_GREEN),
                };

            case GamepadFamily.Switch:
                // Nintendo swaps the letters: the bottom button is B and the right button is A.
                return button switch
                {
                    GamepadButton.South => Letter("B", LIGHT_FILL, DARK_TEXT),
                    GamepadButton.East => Letter("A", LIGHT_FILL, DARK_TEXT),
                    GamepadButton.West => Letter("Y", LIGHT_FILL, DARK_TEXT),
                    _ => Letter("X", LIGHT_FILL, DARK_TEXT),
                };

            case GamepadFamily.Xbox:
                return button switch
                {
                    GamepadButton.South => Letter("A", XBOX_GREEN, LIGHT_TEXT),
                    GamepadButton.East => Letter("B", XBOX_RED, LIGHT_TEXT),
                    GamepadButton.West => Letter("X", XBOX_BLUE, LIGHT_TEXT),
                    _ => Letter("Y", XBOX_YELLOW, DARK_TEXT),
                };

            default:
                return button switch
                {
                    GamepadButton.South => Letter("A", GENERIC_FILL, DARK_TEXT),
                    GamepadButton.East => Letter("B", GENERIC_FILL, DARK_TEXT),
                    GamepadButton.West => Letter("X", GENERIC_FILL, DARK_TEXT),
                    _ => Letter("Y", GENERIC_FILL, DARK_TEXT),
                };
        }
    }

    private static GamepadGlyph Letter(string letter, Color fill, Color textColor)
    {
        return new GamepadGlyph(letter, letter, GlyphSymbol.None, GlyphShape.Circle, fill, textColor);
    }

    private static GamepadGlyph PlayStationSymbol(string name, GlyphSymbol symbol, Color symbolColor)
    {
        return new GamepadGlyph(name, string.Empty, symbol, GlyphShape.Circle, DARK_FILL, symbolColor);
    }

    private static int GetHidVendorId(InputDeviceDescription description)
    {
        if (description.interfaceName != "HID" || string.IsNullOrEmpty(description.capabilities))
        {
            return 0;
        }

        return JsonUtility.FromJson<HID.HIDDeviceDescriptor>(description.capabilities).vendorId;
    }
}
