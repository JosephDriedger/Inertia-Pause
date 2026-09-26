using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public enum ControlScheme
{
    KeyboardMouse,
    Gamepad,
}

/// <summary>
/// Tracks which kind of device the player last used so the UI can show matching control prompts
/// and hand focus to a controller when one is being used.
/// </summary>
public static class InputDeviceMonitor
{
    /// <summary>
    /// Invoked when the player switches between keyboard/mouse and a gamepad.
    /// </summary>
    public static event Action<ControlScheme> OnSchemeChanged;

    /// <summary>
    /// The control scheme the player last used.
    /// </summary>
    public static ControlScheme CurrentScheme { get; private set; } = ControlScheme.KeyboardMouse;

    /// <summary>
    /// The gamepad the player last used, or null if none has been used (or it was disconnected).
    /// </summary>
    public static Gamepad LastGamepad { get; private set; }

    /// <summary>
    /// The device that triggered the most recent input.
    /// </summary>
    public static InputDevice LastDevice { get; private set; }

    public static bool IsGamepad => CurrentScheme == ControlScheme.Gamepad;

    // Ignore stick drift.
    private const float STICK_THRESHOLD = 0.4f;

    // Ignore tiny mouse movements.
    private const float MOUSE_DELTA_THRESHOLD = 0.5f;

    private static bool _isInitialized;

    // Whether the player has used a device yet, as opposed to the scheme being assumed from what is connected.
    private static bool _hasReceivedInput;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Needed for when domain reload is disabled in the editor.
        if (_isInitialized)
        {
            InputSystem.onAfterUpdate -= OnAfterUpdate;
            InputSystem.onDeviceChange -= OnDeviceChange;
        }

        _isInitialized = false;
        _hasReceivedInput = false;
        OnSchemeChanged = null;
        CurrentScheme = ControlScheme.KeyboardMouse;
        LastGamepad = null;
        LastDevice = null;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        // Until the player uses something, assume they will use a connected gamepad rather than showing keyboard prompts.
        if (Gamepad.current != null)
        {
            LastGamepad = Gamepad.current;
            LastDevice = Gamepad.current;
            CurrentScheme = ControlScheme.Gamepad;
        }

        InputSystem.onAfterUpdate += OnAfterUpdate;
        InputSystem.onDeviceChange += OnDeviceChange;

        LogDevices("Input devices at startup");
    }

    // Helps diagnose controllers that Unity does not recognize as a gamepad.
    private static void LogDevices(string title)
    {
        System.Text.StringBuilder devices = new();
        foreach (InputDevice device in InputSystem.devices)
        {
            devices.Append($"\n  {device.displayName} [{device.layout}, {device.GetType().Name}]{(device is Gamepad ? " (gamepad)" : string.Empty)}");

            // List the controls of anything that looks like a controller but is not a gamepad.
            if (device is not Gamepad && device is not Keyboard && device is not Mouse)
            {
                foreach (InputControl control in device.allControls)
                {
                    devices.Append($"\n      {control.path} ({control.layout})");
                }
            }
        }

        Debug.Log($"{title}:{devices}");
    }

    // Runs after every input update, when each device already holds its new state.
    private static void OnAfterUpdate()
    {
        // Check the gamepad last so it wins if both were used in the same update.
        if (IsKeyboardOrMouseUsed(out InputDevice keyboardOrMouse))
        {
            _hasReceivedInput = true;
            LastDevice = keyboardOrMouse;
            SetScheme(ControlScheme.KeyboardMouse, null);
        }

        Gamepad usedGamepad = FindUsedGamepad();
        if (usedGamepad != null)
        {
            _hasReceivedInput = true;
            LastDevice = usedGamepad;
            SetScheme(ControlScheme.Gamepad, usedGamepad);
        }
    }

    private static bool IsKeyboardOrMouseUsed(out InputDevice device)
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard != null && keyboard.anyKey.wasPressedThisFrame)
        {
            device = keyboard;
            return true;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null
            && (mouse.delta.ReadValue().sqrMagnitude > MOUSE_DELTA_THRESHOLD * MOUSE_DELTA_THRESHOLD
                || mouse.leftButton.wasPressedThisFrame
                || mouse.rightButton.wasPressedThisFrame
                || mouse.middleButton.wasPressedThisFrame))
        {
            device = mouse;
            return true;
        }

        device = null;
        return false;
    }

    private static Gamepad FindUsedGamepad()
    {
        foreach (Gamepad gamepad in Gamepad.all)
        {
            if (gamepad.leftStick.ReadValue().sqrMagnitude > STICK_THRESHOLD * STICK_THRESHOLD
                || gamepad.rightStick.ReadValue().sqrMagnitude > STICK_THRESHOLD * STICK_THRESHOLD)
            {
                return gamepad;
            }

            foreach (InputControl control in gamepad.allControls)
            {
                if (control is ButtonControl button && !button.synthetic && button.wasPressedThisFrame)
                {
                    return gamepad;
                }
            }
        }

        return null;
    }

    private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        if (change == InputDeviceChange.Added || change == InputDeviceChange.Reconnected)
        {
            LogDevices($"Input device {change}");

            // A controller that connects before the player has used anything is the one they are about to use.
            if (device is Gamepad newGamepad && !_hasReceivedInput)
            {
                LastDevice = newGamepad;
                SetScheme(ControlScheme.Gamepad, newGamepad);
            }
        }

        if (change != InputDeviceChange.Removed && change != InputDeviceChange.Disconnected)
        {
            return;
        }

        if (device != LastGamepad)
        {
            return;
        }

        LastGamepad = null;

        // Stay on a gamepad on consoles if another one is still connected.
        if (Application.isConsolePlatform && Gamepad.current != null)
        {
            LastGamepad = Gamepad.current;
            LastDevice = Gamepad.current;
            return;
        }

        LastDevice = Keyboard.current;
        SetScheme(ControlScheme.KeyboardMouse, null);
    }

    private static void SetScheme(ControlScheme scheme, Gamepad gamepad)
    {
        // Prompts depend on the kind of controller, so changing controllers counts as a change too.
        bool isGamepadChanged = gamepad != null && gamepad != LastGamepad;
        if (gamepad != null)
        {
            LastGamepad = gamepad;
        }

        if (scheme == CurrentScheme && !isGamepadChanged)
        {
            return;
        }

        CurrentScheme = scheme;
        Debug.Log($"Control scheme changed to {scheme} ({LastDevice?.displayName}).");
        OnSchemeChanged?.Invoke(scheme);
    }
}
