using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionsMenuPresenter : MonoBehaviour
{
    /// <summary>
    /// Invoked when this menu is closed.
    /// </summary>
    public Action OnMenuClose;

    [Header("View")]
    [SerializeField] private OptionsMenuView _view;

    [Header("Models")]
    [SerializeField] private OptionsManager _optionsManager;
    [SerializeField] private GameManager _gameManager;

    private const string OPTION_TRUE_TEXT = "On";
    private const string OPTION_FALSE_TEXT = "Off";
    private const string FOV_FORMAT = "{0}°";
    private const string UNLIMITED_FRAMERATE_TEXT = "Unlimited";
    private const string DEFAULT_SCENARIO_NAME = "Inertia Pause";

    private OptionsModel _dirtyOptionsModel;
    private bool _isSettingHorizontalSensitivity = false;
    private bool _isSettingMusicVolume = false;

    private PlayerActions _inputActions;
    private MenuFocus _focus;

    // The option button whose foldout is currently open, if any.
    private CustomOptionButton _openFoldoutButton;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _view.gameObject.SetActive(false);

        if (_gameManager != null && _gameManager.ScenarioInfo != null)
        {
            _view.LevelNameText.text = _gameManager.ScenarioInfo.ScenarioName;
        }
        else
        {
            _view.LevelNameText.text = DEFAULT_SCENARIO_NAME;
        }

        _view.BackButton.onClick.AddListener(CloseMenu);
        _view.ViewCreditsButton.Button.onClick.AddListener(GoToCredits);

        SetupFoldouts();
        SetupToggles();

        _inputActions = new PlayerActions();
        _focus = MenuFocus.Attach(_view.gameObject, _view.HorizontalSensitivityButton.Button);

        // Back out of an open foldout before leaving the menu.
        _inputActions.UI.Cancel.performed += _ =>
        {
            if (_openFoldoutButton != null)
            {
                CloseFoldoutsAndRestoreFocus();
            }
            else
            {
                CloseMenu();
            }
        };
        _inputActions.UI.Navigate.performed += _ =>
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                _view.HorizontalSensitivityButton.Button.Select();
            }
        };
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);
        _dirtyOptionsModel = _optionsManager.Options.Clone();
        ShowInitialValues(_dirtyOptionsModel);
        _view.DescriptionText.text = string.Empty;
        _inputActions.Enable();
    }

    public void CloseMenu()
    {
        CloseFoldouts();
        _optionsManager.ApplyOptions(_dirtyOptionsModel);
        _optionsManager.SaveOptions(_dirtyOptionsModel);
        _view.gameObject.SetActive(false);
        _inputActions.Disable();
        OnMenuClose?.Invoke();
    }

    private void SetupFoldout(CustomOptionButton button)
    {
        button.Button.onClick.AddListener(() =>
        {
            CloseFoldouts();
            button.OpenFoldout();
            _openFoldoutButton = button;

            // Move a gamepad into the foldout so its options can be picked.
            if (button.Foldout != null)
            {
                _focus.Focus(button.Foldout.GetComponentInChildren<Selectable>());
            }
        });
    }

    private void CloseFoldoutsAndRestoreFocus()
    {
        CustomOptionButton owner = _openFoldoutButton;
        CloseFoldouts();

        if (owner != null)
        {
            _focus.Focus(owner.Button);
        }
    }

    private void CloseFoldouts()
    {
        _openFoldoutButton = null;

        _view.HorizontalSensitivityButton.CloseFoldout();
        _view.VerticalSensitivityButton.CloseFoldout();
        _view.FOVButton.CloseFoldout();
        _view.MaxFramerateButton.CloseFoldout();
        _view.MusicVolumeButton.CloseFoldout();
        _view.SFXVolumeButton.CloseFoldout();
    }

    private void ChangeHint(string description)
    {
        _view.DescriptionText.text = description;
    }

    private void GoToCredits()
    {
        _optionsManager.ApplyOptions(_dirtyOptionsModel);
        _optionsManager.SaveOptions(_dirtyOptionsModel);
        _inputActions.Disable();
        AdditiveSceneManager.Instance.LoadCredits();
    }

    private void ShowInitialValues(OptionsModel options)
    {
        _view.HorizontalSensitivityButton.OptionText.text = options.HorizontalSensitivity.ToString();
        _view.VerticalSensitivityButton.OptionText.text = options.VerticalSensitivity.ToString();
        _view.FOVButton.OptionText.text = string.Format(FOV_FORMAT, options.FieldOfView.ToString());
        _view.MaxFramerateButton.OptionText.text = options.MaxFramerate > 0 ? options.MaxFramerate.ToString() : UNLIMITED_FRAMERATE_TEXT;
        _view.ShowMetricsButton.OptionText.text = options.IsMetricsShown ? OPTION_TRUE_TEXT : OPTION_FALSE_TEXT;
        _view.MusicVolumeButton.OptionText.text = options.MusicVolume.ToString();
        _view.SFXVolumeButton.OptionText.text = options.SoundVolume.ToString();
        _view.ShowObjectTrajectoryButton.OptionText.text = options.IsObjectTrajectoryShown ? OPTION_TRUE_TEXT : OPTION_FALSE_TEXT;
    }

    private void SetupFoldouts()
    {
        // Main option buttons
        SetupFoldout(_view.HorizontalSensitivityButton);
        _view.HorizontalSensitivityButton.Button.onClick.AddListener(() => _isSettingHorizontalSensitivity = true);
        _view.HorizontalSensitivityButton.OnHover += ChangeHint;
        SetupFoldout(_view.VerticalSensitivityButton);
        _view.VerticalSensitivityButton.Button.onClick.AddListener(() => _isSettingHorizontalSensitivity = false);
        _view.VerticalSensitivityButton.OnHover += ChangeHint;

        SetupFoldout(_view.FOVButton);
        _view.FOVButton.OnHover += ChangeHint;
        SetupFoldout(_view.MaxFramerateButton);
        _view.MaxFramerateButton.OnHover += ChangeHint;

        SetupFoldout(_view.MusicVolumeButton);
        _view.MusicVolumeButton.Button.onClick.AddListener(() => _isSettingMusicVolume = true);
        _view.MusicVolumeButton.OnHover += ChangeHint;
        SetupFoldout(_view.SFXVolumeButton);
        _view.SFXVolumeButton.Button.onClick.AddListener(() => _isSettingMusicVolume = false);
        _view.SFXVolumeButton.OnHover += ChangeHint;

        // Sensitivity buttons
        for (int i = 0; i < _view.SensitivityOptions.Count; i++)
        {
            int index = i;
            Button sensitivityButton = _view.SensitivityOptions[index];
            sensitivityButton.onClick.AddListener(() => {
                ChangeSensitivity(index + 1);
                CloseFoldoutsAndRestoreFocus();
            });
        }

        // FOV buttons
        for (int i = 0; i < _view.FOVOptions.Count; i++)
        {
            int index = i;
            Button fovButton = _view.FOVOptions[index];
            fovButton.onClick.AddListener(() => {
                ChangeFieldOfView(index);
                CloseFoldoutsAndRestoreFocus();
            });
        }

        // Framerate buttons
        for (int i = 0; i < _view.FramerateOptions.Count; i++)
        {
            int index = i;
            Button framerateButton = _view.FramerateOptions[index];
            framerateButton.onClick.AddListener(() => {
                ChangeFramerate(index);
                CloseFoldoutsAndRestoreFocus();
            });
        }

        // Volume buttons
        for (int i = 0; i < _view.VolumeOptions.Count; i++)
        {
            int index = i;
            Button volumeButton = _view.VolumeOptions[index];
            volumeButton.onClick.AddListener(() =>
            {
                ChangeVolume(index);
                CloseFoldoutsAndRestoreFocus();
            });
        }
    }

    private void SetupToggles()
    {
        _view.ShowMetricsButton.Button.onClick.AddListener(ToggleShowMetrics);
        _view.ShowMetricsButton.OnHover += ChangeHint;
        _view.ShowObjectTrajectoryButton.Button.onClick.AddListener(ToggleShowObjectTrajectory);
        _view.ShowObjectTrajectoryButton.OnHover += ChangeHint;
    }

    private void ToggleShowMetrics()
    {
        _dirtyOptionsModel.IsMetricsShown = !_dirtyOptionsModel.IsMetricsShown;
        _view.ShowMetricsButton.OptionText.text = _dirtyOptionsModel.IsMetricsShown ? OPTION_TRUE_TEXT : OPTION_FALSE_TEXT;
    }

    private void ToggleShowObjectTrajectory()
    {
        _dirtyOptionsModel.IsObjectTrajectoryShown = !_dirtyOptionsModel.IsObjectTrajectoryShown;
        _view.ShowObjectTrajectoryButton.OptionText.text = _dirtyOptionsModel.IsObjectTrajectoryShown ? OPTION_TRUE_TEXT : OPTION_FALSE_TEXT;
    }

    private void ChangeSensitivity(int sensitivity)
    {
        if (_isSettingHorizontalSensitivity)
        {
            _dirtyOptionsModel.HorizontalSensitivity = sensitivity;
            _view.HorizontalSensitivityButton.OptionText.text = _dirtyOptionsModel.HorizontalSensitivity.ToString();
        }
        else
        {
            _dirtyOptionsModel.VerticalSensitivity = sensitivity;
            _view.VerticalSensitivityButton.OptionText.text = _dirtyOptionsModel.VerticalSensitivity.ToString();
        }
    }

    private void ChangeFramerate(int framerateOptionIndex)
    {
        int framerateOption = framerateOptionIndex switch
        {
            0 => 30,
            1 => 60,
            2 => 120,
            3 => 180,
            4 => 240,
            5 => 360,
            6 => 480,
            7 => -1,
            _ => -1,
        };
        _dirtyOptionsModel.MaxFramerate = framerateOption;

        if (_dirtyOptionsModel.MaxFramerate > 0)
        {
            _view.MaxFramerateButton.OptionText.text = _dirtyOptionsModel.MaxFramerate.ToString();
        }
        else
        {
            _view.MaxFramerateButton.OptionText.text = UNLIMITED_FRAMERATE_TEXT;
        }
    }

    private void ChangeFieldOfView(int fieldOfViewOptionIndex)
    {
        int fieldOfViewOption = fieldOfViewOptionIndex switch
        {
            0 => 60,
            1 => 70,
            2 => 80,
            3 => 90,
            4 => 100,
            5 => 110,
            6 => 120,
            _ => 80,
        };
        _dirtyOptionsModel.FieldOfView = fieldOfViewOption;
        _view.FOVButton.OptionText.text = string.Format(FOV_FORMAT, _dirtyOptionsModel.FieldOfView.ToString());
    }

    private void ChangeVolume(int volume)
    {
        if (_isSettingMusicVolume)
        {
            _dirtyOptionsModel.MusicVolume = volume;
            _view.MusicVolumeButton.OptionText.text = _dirtyOptionsModel.MusicVolume.ToString();
        }
        else
        {
            _dirtyOptionsModel.SoundVolume = volume;
            _view.SFXVolumeButton.OptionText.text = _dirtyOptionsModel.SoundVolume.ToString();
        }
    }
}
