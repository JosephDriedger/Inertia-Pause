using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Looks up the text to show for an input action, for whichever control scheme is in use.
/// </summary>
public static class ControlPromptLabels
{
    private const string GAMEPAD_PATH_PREFIX = "<Gamepad>";
    private const string KEYBOARD_PATH_PREFIX = "<Keyboard>";
    private const string MOUSE_PATH_PREFIX = "<Mouse>";

    // UI Submit and Cancel are bound by usage rather than by path, so these mirror what the UI module uses for a gamepad.
    private const string GAMEPAD_SUBMIT_PATH = "<Gamepad>/buttonSouth";
    private const string GAMEPAD_CANCEL_PATH = "<Gamepad>/buttonEast";

    // Only used to read bindings; never enabled.
    private static PlayerActions _bindingSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        _bindingSource = null;
    }

    public static string GamepadSubmitLabel => ResolveGlyph(GAMEPAD_SUBMIT_PATH).Name;

    public static string GamepadCancelLabel => ResolveGlyph(GAMEPAD_CANCEL_PATH).Name;

    /// <summary>
    /// How the action's gamepad button looks on the controller the player last used, or null if the action has no gamepad binding.
    /// </summary>
    /// <param name="actionPath">Action in the form "MapName/ActionName".</param>
    public static GamepadGlyph? GetGamepadGlyph(string actionPath)
    {
        string controlPath = GetBindingPath(actionPath, GAMEPAD_PATH_PREFIX);
        return controlPath != null ? ResolveGlyph(controlPath) : (GamepadGlyph?)null;
    }

    /// <summary>
    /// Label for the action on the gamepad the player last used, e.g. "A" on an Xbox controller and "Cross" on a PlayStation controller.
    /// Returns null if the action has no gamepad binding.
    /// </summary>
    public static string GetGamepadLabel(string actionPath)
    {
        return GetGamepadGlyph(actionPath)?.Name;
    }

    /// <summary>
    /// Label for the action's keyboard binding, e.g. "TAB". Falls back to the mouse binding, or null if there is none.
    /// </summary>
    public static string GetKeyboardLabel(string actionPath)
    {
        string controlPath = GetBindingPath(actionPath, KEYBOARD_PATH_PREFIX) ?? GetBindingPath(actionPath, MOUSE_PATH_PREFIX);
        return controlPath != null
            ? InputControlPath.ToHumanReadableString(controlPath, InputControlPath.HumanReadableStringOptions.OmitDevice).ToUpperInvariant()
            : null;
    }

    /// <summary>
    /// Label for the action for the control scheme currently in use.
    /// </summary>
    public static string GetCurrentLabel(string actionPath)
    {
        return InputDeviceMonitor.IsGamepad ? GetGamepadLabel(actionPath) : GetKeyboardLabel(actionPath);
    }

    /// <summary>
    /// Replaces "{Map/Action}" tokens in the text with the label for the control scheme currently in use.
    /// Tokens for actions with no binding for that scheme are left as is.
    /// </summary>
    public static string ReplaceTokens(string text)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains('{'))
        {
            return text;
        }

        StringBuilder result = new(text.Length);
        int index = 0;
        while (index < text.Length)
        {
            int start = text.IndexOf('{', index);
            int end = start < 0 ? -1 : text.IndexOf('}', start);
            if (end < 0)
            {
                result.Append(text, index, text.Length - index);
                break;
            }

            result.Append(text, index, start - index);
            string token = text.Substring(start + 1, end - start - 1);
            result.Append(GetCurrentLabel(token) ?? text.Substring(start, end - start + 1));
            index = end + 1;
        }

        return result.ToString();
    }

    // Describes the gamepad control for the controller the player last used.
    private static GamepadGlyph ResolveGlyph(string controlPath)
    {
        Gamepad gamepad = InputDeviceMonitor.LastGamepad ?? Gamepad.current;
        return GamepadGlyphs.ForPath(controlPath, GamepadGlyphs.GetFamily(gamepad));
    }

    // The path of the action's first binding for the device type, e.g. "<Keyboard>/tab".
    private static string GetBindingPath(string actionPath, string pathPrefix)
    {
        if (string.IsNullOrEmpty(actionPath))
        {
            return null;
        }

        _bindingSource ??= new PlayerActions();
        InputAction action = _bindingSource.asset.FindAction(actionPath);
        if (action == null)
        {
            Debug.LogWarning($"No input action found for control prompt \"{actionPath}\".");
            return null;
        }

        foreach (InputBinding binding in action.bindings)
        {
            if (!binding.isComposite && !binding.isPartOfComposite && binding.effectivePath.StartsWith(pathPrefix))
            {
                return binding.effectivePath;
            }
        }

        return null;
    }
}
