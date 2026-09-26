using UnityEngine;
using UnityEngine.EventSystems;

public class TutorialPanelPresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private TutorialPanelView _view;

    [Header("Models")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private PlayerInteract _playerInteractModel;

    private PlayerActions _inputActions;

    private const string PAGE_NUMBER_FORMAT = "{0} / {1}";

    private TutorialInfo _currentTutorial;
    private int _currentPageIndex;
    private bool _isOpeningTutorial = false;

    void Start()
    {
        _view.gameObject.SetActive(false);

        _gameManager.OnLevelStart += OnLevelStart;

        _view.BackButton.onClick.AddListener(OnBackClicked);
        _view.StartScenarioButton.onClick.AddListener(OnBackClicked);
        _view.PrevPageButton.onClick.AddListener(OnPrevClicked);
        _view.NextPageButton.onClick.AddListener(OnNextClicked);

        _playerInteractModel.OnTutorialOpen += tutorial => ShowTutorial(tutorial);

        // Tutorial text can reference controls (e.g. "{Ingame/ContextualHelp}"), which depend on the device in use.
        InputDeviceMonitor.OnSchemeChanged += OnSchemeChanged;

        // UI
        MenuFocus.Attach(_view.gameObject, _view.NextPageButton, _view.StartScenarioButton, _view.BackButton);

        _inputActions = new PlayerActions();
        _inputActions.UI.Cancel.performed += _ => OnBackClicked();
        _inputActions.Ingame.ContextualHelp.performed += _ => OnBackClicked();
    }

    private void OnDestroy()
    {
        InputDeviceMonitor.OnSchemeChanged -= OnSchemeChanged;
    }

    private void OnSchemeChanged(ControlScheme _)
    {
        if (_currentTutorial != null)
        {
            DisplayContentText();
        }
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);
        _gameManager.AnyBlockingMenuOpened();

        if (!_isOpeningTutorial)
        {
            _inputActions.UI.Enable();
            _inputActions.Ingame.ContextualHelp.Enable();
        }
    }

    public void CloseMenu()
    {
        _view.gameObject.SetActive(false);
        _gameManager.AnyBlockingMenuClosed();
        _inputActions.UI.Disable();
        _inputActions.Ingame.ContextualHelp.Disable();
        EventSystem.current.SetSelectedGameObject(null);
    }

    public void ShowTutorial(TutorialInfo tutorial, bool isOpeningTutorial = false)
    {
        if (tutorial == null)
        {
            Debug.LogWarning("This object does not have a tutorial!");
            return;
        }

        _isOpeningTutorial = isOpeningTutorial;

        _view.BackButton.gameObject.SetActive(!_isOpeningTutorial);
        _view.StartScenarioButton.gameObject.SetActive(false);

        SetTutorial(tutorial);
        OpenMenu();
    }

    private void OnLevelStart()
    {
        TutorialInfo openingTutorial = _gameManager.ScenarioInfo.OpeningTutorial;
        if (openingTutorial != null)
        {
            ShowTutorial(openingTutorial, isOpeningTutorial: true);
        }
    }

    private void SetTutorial(TutorialInfo tutorial)
    {
        _currentTutorial = tutorial;
        _view.Header.text = tutorial.ObjectName;

        DisplayPage(0);
    }

    private void DisplayPage(int index)
    {
        _currentPageIndex = index;
        DisplayContentText();
        _view.ContentImage.sprite = _currentTutorial.Pages[index].Image;

        bool isFirstPage = index == 0;
        bool isLastPage = index == _currentTutorial.Pages.Count - 1;

        _view.PrevPageButton.gameObject.SetActive(!isFirstPage);
        _view.NextPageButton.gameObject.SetActive(!isLastPage);

        if (_isOpeningTutorial && isLastPage)
        {
            _view.StartScenarioButton.gameObject.SetActive(true);
            _inputActions.UI.Enable();
            _inputActions.Ingame.ContextualHelp.Enable();
        }

        if (_currentTutorial.Pages.Count > 1)
        {
            _view.PageText.text = string.Format(PAGE_NUMBER_FORMAT, _currentPageIndex + 1, _currentTutorial.Pages.Count);
        }
        else
        {
            _view.PageText.text = string.Empty;
        }
    }

    private void DisplayContentText()
    {
        _view.ContentText.text = ControlPromptLabels.ReplaceTokens(_currentTutorial.Pages[_currentPageIndex].Content);
    }

    private void OnPrevClicked()
    {
        if (_currentPageIndex > 0)
        {
            DisplayPage(_currentPageIndex - 1);
        }
    }

    private void OnNextClicked()
    {
        if (_currentPageIndex < _currentTutorial.Pages.Count - 1)
        {
            DisplayPage(_currentPageIndex + 1);
        }
    }

    private void OnBackClicked()
    {
        CloseMenu();
    }
}
