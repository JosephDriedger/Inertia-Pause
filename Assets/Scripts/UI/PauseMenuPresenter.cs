using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

public class PauseMenuPresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private PauseMenuView _view;
    [SerializeField] private OptionsMenuPresenter _optionsPresenter;

    [Header("Models")]
    [SerializeField] private GameManager _gameManager;

    private PlayerActions _inputActions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _view.gameObject.SetActive(false);

        if (_gameManager.ScenarioInfo != null)
        {
            DisplayScenarioInfo(_gameManager.ScenarioInfo);
        }

        // Pause menu
        _view.ResumeButton.Button.onClick.AddListener(OnResumePressed);
        _view.RestartButton.Button.onClick.AddListener(OnRestartPressed);
        _view.OptionsButton.Button.onClick.AddListener(OnOptionsPressed);
        _view.QuitScenarioButton.Button.onClick.AddListener(OnQuitPressed);
        _view.BackButton.Button.onClick.AddListener(OnResumePressed);

        _view.ResumeButton.OnHover += ChangeHint;
        _view.RestartButton.OnHover += ChangeHint;
        _view.OptionsButton.OnHover += ChangeHint;
        _view.QuitScenarioButton.OnHover += ChangeHint;
        _view.BackButton.OnHover += ChangeHint;

        // Options menu
        _optionsPresenter.OnMenuClose += OpenMenu;

        // GameManager
        _gameManager.OnPauseMenuOpen += OpenMenu;

        // UI
        MenuFocus.Attach(_view.gameObject, _view.ResumeButton.Button);

        _inputActions = new PlayerActions();
        _inputActions.UI.Cancel.performed += _ => OnResumePressed();
        _inputActions.UI.Navigate.performed += _ =>
        {
            if (EventSystem.current.currentSelectedGameObject == null)
            {
                _view.ResumeButton.Button.Select();
            }
        };
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);

        _view.ActionsTakenText.text = _gameManager.ActionCount.ToString();
        _view.DescriptionText.text = string.Empty;
        _inputActions.UI.Enable();
    }

    public void CloseMenu()
    {
        _view.gameObject.SetActive(false);
        _inputActions.UI.Disable();
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void ChangeHint(string description)
    {
        _view.DescriptionText.text = description;
    }

    private void DisplayScenarioInfo(ScenarioInfo scenarioInfo)
    {
        _view.LevelNameText.text = scenarioInfo.ScenarioName;

        IEnumerable<string> scenarioObjectivesBulletPoints = scenarioInfo.Objectives.MainObjectives.Select(objective => $"- {objective}");
        _view.ScenarioObjectivesText.text = string.Join("\n", scenarioObjectivesBulletPoints);

        IEnumerable<string> optionalObjectivesBulletPoints = scenarioInfo.Objectives.OptionalObjectives.Select(objective => $"- {objective.Description}");
        _view.OptionalObjectivesText.text = string.Join("\n", optionalObjectivesBulletPoints);
    }

    private void OnResumePressed()
    {
        CloseMenu();
        _gameManager.AnyBlockingMenuClosed();
    }

    private void OnRestartPressed()
    {
        CloseMenu();
        AdditiveSceneManager.Instance.ReloadScenario();
    }

    private void OnOptionsPressed()
    {
        _view.gameObject.SetActive(false);
        _optionsPresenter.OpenMenu();
        _inputActions.UI.Disable();
    }

    private void OnQuitPressed()
    {
        CloseMenu();
        AdditiveSceneManager.Instance.LoadMainMenu();
    }
}
