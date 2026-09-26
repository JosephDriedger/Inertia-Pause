using UnityEditor;
using UnityEngine;
using static UnityEngine.InputSystem.InputAction;

public class MainMenuPresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private MainMenuView _view;

    [Header("Models")]
    [SerializeField] private LevelSelectPresenter _levelSelectPresenter;
    [SerializeField] private OptionsMenuPresenter _optionsMenuPresenter;
    [SerializeField] private SavedLevelProgressManager _progressManager;

    private const string BUILD_NUMBER_FORMAT = "{0} V{1}";
    private const string FIRST_LEVEL_ENVIRONMENT = "1-promenade";
    private const string NORMAL_FIRST_LEVEL_SCENARIO_ASSETS = "1-promenade-easy";
    private const string HARD_FIRST_LEVEL_SCENARIO_ASSETS = "1-promenade-hard";
    private const string GAMEPAD_START_TEXT_FORMAT = "Press {0} to Begin";

    private AdditiveSceneManager _sceneManager;
    private PlayerActions _inputActions;
    private MenuFocus _focus;
    private string _keyboardStartText;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _sceneManager = AdditiveSceneManager.Instance;

        _view.gameObject.SetActive(true);
        _view.MainMenuScreen.SetActive(false);
        _view.DifficultyPopup.SetActive(false);
        _view.StartScreen.SetActive(true);

        _view.BuildText.text = string.Format(BUILD_NUMBER_FORMAT, Application.platform, Application.version);
        _view.DescriptionText.text = string.Empty;

        // Hide Continue button for first time players.
        bool isContinueAvailable = _progressManager.LevelProgressData.CurrentLevelAssetsName != null && _progressManager.LevelProgressData.CurrentLevelAssetsName != string.Empty;
        _view.ContinueButton.gameObject.SetActive(isContinueAvailable);

        _optionsMenuPresenter.OnMenuClose += () => OpenMenu();
        _levelSelectPresenter.OnMenuClose += () => OpenMenu();

        _view.ContinueButton.Button.onClick.AddListener(OnContinueClicked);
        _view.NewGameButton.Button.onClick.AddListener(OnNewGameClicked);
        _view.ScenarioSelectButton.Button.onClick.AddListener(OnScenarioSelectClicked);
        _view.OptionsButton.Button.onClick.AddListener(OnOptionsClicked);
        _view.ExitButton.Button.onClick.AddListener(OnExitClicked);

        _view.NormalDifficultyButton.Button.onClick.AddListener(OnNewGamePopupNormalClicked);
        _view.HardDifficultyButton.Button.onClick.AddListener(OnNewGamePopupHardClicked);
        _view.DifficultyBackButton.Button.onClick.AddListener(OnNewGamePopupBackClicked);

        _view.ContinueButton.OnHover += ChangeHint;
        _view.NewGameButton.OnHover += ChangeHint;
        _view.ScenarioSelectButton.OnHover += ChangeHint;
        _view.OptionsButton.OnHover += ChangeHint;
        _view.ExitButton.OnHover += ChangeHint;

        _focus = MenuFocus.Attach(_view.gameObject, _view.ContinueButton.Button, _view.NewGameButton.Button);

        _keyboardStartText = _view.StartText.text;
        UpdateStartText();

        _inputActions = new PlayerActions();
        _inputActions.UI.Click.performed += GoToMainMenu;
        _inputActions.UI.Submit.performed += GoToMainMenu;
        _inputActions.UI.Cancel.performed += OnCancelPerformed;
        _inputActions.Enable();
    }

    private void Update()
    {
        // The prompt depends on the controls in use, and a controller can be plugged in at any time.
        if (_view.StartScreen.activeInHierarchy)
        {
            UpdateStartText();
        }
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);
        _view.DescriptionText.text = string.Empty;
        _inputActions.Enable();
    }

    public void CloseMenu()
    {
        _view.gameObject.SetActive(false);
        _inputActions.Disable();
    }

    private void ChangeHint(string description)
    {
        _view.DescriptionText.text = description;
    }

    private void UpdateStartText()
    {
        string text = InputDeviceMonitor.IsGamepad
            ? string.Format(GAMEPAD_START_TEXT_FORMAT, ControlPromptLabels.GamepadSubmitLabel)
            : _keyboardStartText;

        if (_view.StartText.text != text)
        {
            _view.StartText.text = text;
        }
    }

    // Back out of the difficulty popup.
    private void OnCancelPerformed(CallbackContext _)
    {
        if (_view.DifficultyPopup.activeInHierarchy)
        {
            OnNewGamePopupBackClicked();
        }
    }

    private void GoToMainMenu(CallbackContext _)
    {
        _view.MainMenuScreen.SetActive(true);
        _view.StartScreen.SetActive(false);

        _inputActions.UI.Click.performed -= GoToMainMenu;
        _inputActions.UI.Submit.performed -= GoToMainMenu;
    }

    private void OnContinueClicked()
    {
        _sceneManager.LoadScenario(_progressManager.LevelProgressData.CurrentLevelEnvironmentName, _progressManager.LevelProgressData.CurrentLevelAssetsName);
    }

    private void OnNewGameClicked()
    {
        _view.DifficultyPopup.SetActive(true);
        _view.BottomBar.SetActive(false);
        _focus.OpenPopup(_view.DifficultyPopup, _view.NormalDifficultyButton.Button);
    }

    private void OnNewGamePopupNormalClicked()
    {
        _sceneManager.LoadScenario(FIRST_LEVEL_ENVIRONMENT, NORMAL_FIRST_LEVEL_SCENARIO_ASSETS);
    }

    private void OnNewGamePopupHardClicked()
    {
        _sceneManager.LoadScenario(FIRST_LEVEL_ENVIRONMENT, HARD_FIRST_LEVEL_SCENARIO_ASSETS);
    }

    private void OnNewGamePopupBackClicked()
    {
        _view.DifficultyPopup.SetActive(false);
        _view.BottomBar.SetActive(true);
        _focus.ClosePopup();
    }

    private void OnScenarioSelectClicked()
    {
        _view.gameObject.SetActive(false);
        _levelSelectPresenter.OpenMenu();
    }

    private void OnOptionsClicked()
    {
        _view.gameObject.SetActive(false);
        _optionsMenuPresenter.OpenMenu();
    }

    private void OnExitClicked()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit(0);
#endif
    }
}
