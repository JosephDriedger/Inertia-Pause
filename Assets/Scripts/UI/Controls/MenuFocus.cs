using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Keeps a menu usable with a gamepad. Menus are mouse-driven by default, so nothing is selected when they open;
/// this selects a button while a gamepad is in use and clears the highlight again once the mouse takes over.
/// Added to a menu's root object at runtime by its presenter.
/// </summary>
public class MenuFocus : MonoBehaviour
{
    // Candidates for the initial selection, in priority order.
    private Selectable[] _defaultCandidates;
    private GameObject _lastSelected;

    // Selectables outside of the active popup and the navigation they had before the popup opened.
    private readonly Dictionary<Selectable, Navigation> _suspendedNavigation = new();
    private GameObject _popup;
    private GameObject _selectedBeforePopup;

    /// <summary>
    /// Adds focus handling to the menu, or updates it if the menu already has it.
    /// </summary>
    /// <param name="menuRoot">The object that is activated and deactivated when the menu opens and closes.</param>
    /// <param name="defaultCandidates">Buttons to select when the menu opens, in priority order. The first one that is active is used.</param>
    public static MenuFocus Attach(GameObject menuRoot, params Selectable[] defaultCandidates)
    {
        if (!menuRoot.TryGetComponent(out MenuFocus focus))
        {
            focus = menuRoot.AddComponent<MenuFocus>();
        }

        focus._defaultCandidates = defaultCandidates;
        return focus;
    }

    /// <summary>
    /// Moves the selection to the button. Nothing is highlighted while the mouse is in use, so this only
    /// takes effect when a gamepad is; the button is remembered either way.
    /// </summary>
    public void Focus(Selectable target)
    {
        if (target == null)
        {
            return;
        }

        _lastSelected = target.gameObject;
        if (InputDeviceMonitor.IsGamepad && isActiveAndEnabled && IsSelectable(target.gameObject))
        {
            Select(target.gameObject);
        }
    }

    /// <summary>
    /// Restricts navigation to the popup until <see cref="ClosePopup"/> is called, so a gamepad cannot
    /// move to buttons hidden behind it, and selects the given button in it.
    /// </summary>
    public void OpenPopup(GameObject popup, Selectable firstSelected)
    {
        ClosePopup(restoreFocus: false);

        _popup = popup;
        _selectedBeforePopup = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;

        foreach (Selectable selectable in GetComponentsInChildren<Selectable>(includeInactive: true))
        {
            if (selectable.transform.IsChildOf(popup.transform))
            {
                continue;
            }

            _suspendedNavigation[selectable] = selectable.navigation;
            Navigation navigation = selectable.navigation;
            navigation.mode = Navigation.Mode.None;
            selectable.navigation = navigation;
        }

        Focus(firstSelected);
    }

    /// <summary>
    /// Ends the restriction started by <see cref="OpenPopup"/> and returns to what was selected before it.
    /// </summary>
    public void ClosePopup(bool restoreFocus = true)
    {
        foreach (KeyValuePair<Selectable, Navigation> suspended in _suspendedNavigation)
        {
            if (suspended.Key != null)
            {
                suspended.Key.navigation = suspended.Value;
            }
        }
        _suspendedNavigation.Clear();

        if (_popup == null)
        {
            return;
        }
        _popup = null;

        if (restoreFocus && _selectedBeforePopup != null && _selectedBeforePopup.TryGetComponent(out Selectable previous))
        {
            Focus(previous);
        }
        _selectedBeforePopup = null;
    }

    private void OnEnable()
    {
        InputDeviceMonitor.OnSchemeChanged += OnSchemeChanged;
    }

    private void OnDisable()
    {
        InputDeviceMonitor.OnSchemeChanged -= OnSchemeChanged;

        // Any popup is closed with the menu.
        ClosePopup(restoreFocus: false);
    }

    private void Update()
    {
        if (!InputDeviceMonitor.IsGamepad || EventSystem.current == null)
        {
            return;
        }

        // Runs every frame so focus recovers from anything that drops it: the menu opening, a popup or page
        // hiding the selected button, or a click on the background.
        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(transform) && IsSelectable(selected))
        {
            _lastSelected = selected;
            return;
        }

        GameObject target = FindTarget();
        if (target != null)
        {
            Select(target);
        }
    }

    private void OnSchemeChanged(ControlScheme scheme)
    {
        // Pressing a keyboard key should keep the selection for keyboard navigation; only the mouse clears it.
        if (scheme != ControlScheme.KeyboardMouse || InputDeviceMonitor.LastDevice is not UnityEngine.InputSystem.Pointer || EventSystem.current == null)
        {
            return;
        }

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected != null && selected.transform.IsChildOf(transform))
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    private GameObject FindTarget()
    {
        if (_lastSelected != null && IsSelectable(_lastSelected))
        {
            return _lastSelected;
        }

        if (_defaultCandidates == null)
        {
            return null;
        }

        foreach (Selectable candidate in _defaultCandidates)
        {
            if (candidate != null && IsSelectable(candidate.gameObject))
            {
                return candidate.gameObject;
            }
        }

        return null;
    }

    private bool IsSelectable(GameObject target)
    {
        if (!target.activeInHierarchy || !target.transform.IsChildOf(transform))
        {
            return false;
        }

        // While a popup is open, only its buttons can be selected.
        if (_popup != null && !target.transform.IsChildOf(_popup.transform))
        {
            return false;
        }

        return !target.TryGetComponent(out Selectable selectable) || selectable.IsInteractable();
    }

    private static void Select(GameObject target)
    {
        EventSystem.current.SetSelectedGameObject(target);
    }
}
