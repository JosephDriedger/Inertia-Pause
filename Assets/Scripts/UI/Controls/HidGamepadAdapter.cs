using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.InputSystem.HID;
using UnityEngine.InputSystem.LowLevel;

/// <summary>
/// The Input System does not recognize some Xbox controllers connected over Bluetooth as gamepads; they show up as a
/// generic HID joystick, so none of the gamepad bindings work. This mirrors such a controller onto a virtual gamepad
/// so the rest of the game (menus, prompts, gameplay) treats it like any other gamepad.
/// </summary>
/// <remarks>
/// The trigger axes are not exposed by the generic HID device, so the triggers are not mirrored.
/// </remarks>
public static class HidGamepadAdapter
{
    private const int MICROSOFT_VENDOR_ID = 0x045E;

    // Xbox Wireless Controller (Series X|S) over Bluetooth.
    private static readonly int[] SUPPORTED_PRODUCT_IDS = { 0x0B13 };

    private const string VIRTUAL_GAMEPAD_NAME = "Xbox Wireless Controller (Bluetooth)";

    // HID button numbers of the Xbox Wireless Controller and the gamepad buttons they map to.
    // The generic HID device names button 1 "trigger" and the rest "buttonN".
    private static readonly (string Control, GamepadButton Button)[] BUTTON_MAP =
    {
        ("trigger", GamepadButton.South),   // A
        ("button2", GamepadButton.East),    // B
        ("button4", GamepadButton.West),    // X
        ("button5", GamepadButton.North),   // Y
        ("button7", GamepadButton.LeftShoulder),
        ("button8", GamepadButton.RightShoulder),
        ("button11", GamepadButton.Select), // View
        ("button12", GamepadButton.Start),  // Menu
        ("button14", GamepadButton.LeftStick),
        ("button15", GamepadButton.RightStick),
    };

    private class Link
    {
        public Joystick Source;
        public Gamepad Virtual;
        public GamepadState LastState;
        public bool HasSentState;
        public StickControl LeftStick;
        public AxisControl RightStickX;
        public AxisControl RightStickY;
        public DpadControl Hat;
        public ButtonControl[] Buttons;
    }

    private static readonly List<Link> _links = new();
    private static bool _isInitialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        // Needed for when domain reload is disabled in the editor.
        if (_isInitialized)
        {
            InputSystem.onDeviceChange -= OnDeviceChange;
            InputSystem.onBeforeUpdate -= OnBeforeUpdate;
        }

        _isInitialized = false;
        _links.Clear();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (_isInitialized)
        {
            return;
        }
        _isInitialized = true;

        foreach (InputDevice device in InputSystem.devices)
        {
            TryLink(device);
        }

        InputSystem.onDeviceChange += OnDeviceChange;
        InputSystem.onBeforeUpdate += OnBeforeUpdate;
    }

    private static void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        switch (change)
        {
            case InputDeviceChange.Added:
            case InputDeviceChange.Reconnected:
                TryLink(device);
                break;
            case InputDeviceChange.Removed:
            case InputDeviceChange.Disconnected:
                Unlink(device);
                break;
        }
    }

    /// <summary>
    /// Whether the gamepad is a virtual one that mirrors a controller Unity does not recognize as a gamepad.
    /// </summary>
    public static bool IsVirtualGamepad(Gamepad gamepad)
    {
        return _links.Exists(link => link.Virtual == gamepad);
    }

    private static bool IsSupported(InputDevice device)
    {
        if (device is Gamepad || device is not Joystick || device.description.interfaceName != "HID")
        {
            return false;
        }

        if (string.IsNullOrEmpty(device.description.capabilities))
        {
            return false;
        }

        HID.HIDDeviceDescriptor descriptor = JsonUtility.FromJson<HID.HIDDeviceDescriptor>(device.description.capabilities);
        if (descriptor.vendorId != MICROSOFT_VENDOR_ID)
        {
            return false;
        }

        foreach (int productId in SUPPORTED_PRODUCT_IDS)
        {
            if (descriptor.productId == productId)
            {
                return true;
            }
        }

        return false;
    }

    private static void TryLink(InputDevice device)
    {
        if (!IsSupported(device) || _links.Exists(link => link.Source == device))
        {
            return;
        }

        Joystick source = (Joystick)device;
        Link link = new()
        {
            Source = source,
            LeftStick = source.stick,
            RightStickX = source.TryGetChildControl<AxisControl>("z"),
            RightStickY = source.TryGetChildControl<AxisControl>("rz"),
            Hat = source.TryGetChildControl<DpadControl>("hat"),
            Buttons = new ButtonControl[BUTTON_MAP.Length],
        };

        for (int i = 0; i < BUTTON_MAP.Length; i++)
        {
            link.Buttons[i] = source.TryGetChildControl<ButtonControl>(BUTTON_MAP[i].Control);
        }

        link.Virtual = InputSystem.AddDevice<Gamepad>(VIRTUAL_GAMEPAD_NAME);
        _links.Add(link);
        Debug.Log($"Mirroring {source.displayName} onto a virtual gamepad.");
    }

    private static void Unlink(InputDevice device)
    {
        int index = _links.FindIndex(link => link.Source == device);
        if (index < 0)
        {
            return;
        }

        Link link = _links[index];
        _links.RemoveAt(index);
        if (link.Virtual != null && link.Virtual.added)
        {
            InputSystem.RemoveDevice(link.Virtual);
        }
    }

    // Queue the mirrored state so it is processed in the update that is about to run.
    private static void OnBeforeUpdate()
    {
        foreach (Link link in _links)
        {
            GamepadState state = ReadState(link);
            if (link.HasSentState && Matches(state, link.LastState))
            {
                continue;
            }

            link.LastState = state;
            link.HasSentState = true;
            InputSystem.QueueStateEvent(link.Virtual, state);
        }
    }

    private static GamepadState ReadState(Link link)
    {
        GamepadState state = default;

        state.leftStick = link.LeftStick.ReadValue();

        // The generic HID device reports the right stick as the Z and Rz axes, with Rz pointing down.
        float rightX = link.RightStickX != null ? link.RightStickX.ReadValue() : 0f;
        float rightY = link.RightStickY != null ? -link.RightStickY.ReadValue() : 0f;
        state.rightStick = new Vector2(rightX, rightY);

        for (int i = 0; i < BUTTON_MAP.Length; i++)
        {
            ButtonControl button = link.Buttons[i];
            if (button != null && button.isPressed)
            {
                state.WithButton(BUTTON_MAP[i].Button);
            }
        }

        if (link.Hat != null)
        {
            state.WithButton(GamepadButton.DpadUp, link.Hat.up.isPressed);
            state.WithButton(GamepadButton.DpadDown, link.Hat.down.isPressed);
            state.WithButton(GamepadButton.DpadLeft, link.Hat.left.isPressed);
            state.WithButton(GamepadButton.DpadRight, link.Hat.right.isPressed);
        }

        return state;
    }

    private static bool Matches(GamepadState a, GamepadState b)
    {
        return a.buttons == b.buttons
            && (a.leftStick - b.leftStick).sqrMagnitude < 0.0001f
            && (a.rightStick - b.rightStick).sqrMagnitude < 0.0001f;
    }
}
