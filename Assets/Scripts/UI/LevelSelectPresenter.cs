using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class LevelSelectPresenter : MonoBehaviour
{
    public Action OnMenuClose;

    [Header("View")]
    [SerializeField] private LevelSelectView _view;

    [Header("Models")]
    [SerializeField] private SavedLevelProgressManager _progressManager;

    private const string NORMAL_DIFFICULTY_NAME = "Normal";
    private const string HARD_DIFFICULTY_NAME = "Hard";
    private const string BEST_RECORD_SINGULAR_FORMAT = "{0} Best Record: {1} Action";
    private const string BEST_RECORD_PLURAL_FORMAT = "{0} Best Record: {1} Actions";
    private const string NO_RECORD_FORMAT = "{0} Best Record: Incomplete";

    private AdditiveSceneManager _sceneManager;
    private PlayerActions _inputActions;
    private MenuFocus _focus;

    private ScenarioInfo _normalScenarioToBeLoaded;
    private ScenarioInfo _hardScenarioToBeLoaded;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _sceneManager = AdditiveSceneManager.Instance;

        _view.gameObject.SetActive(false);
        _view.DifficultyPopup.gameObject.SetActive(false);

        _view.LevelSelectButtons[0].Button.Select();

        // Level select
        foreach (CustomLevelSelectButtonView button in _view.LevelSelectButtons)
        {
            button.OnHoverLevel += OnLevelSelectButtonHover;
            button.OnConfirmLevel += OnLevelSelectButtonClick;
        }

        _view.BackButton.Button.onClick.AddListener(CloseMenu);

        // Difficulty popup
        _view.DifficultyBackButton.Button.onClick.AddListener(CloseDifficultySelect);
        _view.NormalDifficultyButton.Button.onClick.AddListener(LoadNormalScenario);
        _view.HardDifficultyButton.Button.onClick.AddListener(LoadHardScenario);

        // Gamepad focus starts on the first level, then the back button.
        Selectable[] defaultCandidates = _view.LevelSelectButtons
            .Select(levelButton => (Selectable)levelButton.Button)
            .Append(_view.BackButton.Button)
            .ToArray();
        _focus = MenuFocus.Attach(_view.gameObject, defaultCandidates);

        _inputActions = new PlayerActions();
        _inputActions.UI.Cancel.performed += _ => OnCancelPerformed();
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);
        _view.DifficultyPopup.gameObject.SetActive(false);
        _inputActions.UI.Enable();
    }

    public void CloseMenu()
    {
        _view.gameObject.SetActive(false);
        _inputActions.UI.Disable();
        OnMenuClose?.Invoke();
    }

    // Back out of the difficulty popup first, then the menu.
    private void OnCancelPerformed()
    {
        if (_view.DifficultyPopup.activeInHierarchy)
        {
            CloseDifficultySelect();
        }
        else
        {
            CloseMenu();
        }
    }

    private void LoadNormalScenario()
    {
        if (_normalScenarioToBeLoaded == null)
        {
            Debug.LogError("No scene is set to be loaded.");
            return;
        }

        ScenarioInfo sceneToLoad = _normalScenarioToBeLoaded;

        if (sceneToLoad.EnvironmentSceneName == string.Empty || sceneToLoad.ScenarioAssetsSceneName == string.Empty)
        {
            Debug.LogError("Scene to be loaded is empty string.");
            return;
        }

        _sceneManager.LoadScenario(sceneToLoad.EnvironmentSceneName, sceneToLoad.ScenarioAssetsSceneName);
    }

    private void LoadHardScenario()
    {
        if (_hardScenarioToBeLoaded == null)
        {
            Debug.LogError("No scene is set to be loaded.");
            return;
        }

        ScenarioInfo sceneToLoad = _hardScenarioToBeLoaded;

        if (sceneToLoad.EnvironmentSceneName == string.Empty || sceneToLoad.ScenarioAssetsSceneName == string.Empty)
        {
            Debug.LogError("Scene to be loaded is empty string.");
            return;
        }

        _sceneManager.LoadScenario(sceneToLoad.EnvironmentSceneName, sceneToLoad.ScenarioAssetsSceneName);
    }

    private void OnLevelSelectButtonHover(ScenarioInfo normalScenarioInfo, ScenarioInfo hardScenarioInfo)
    {
        _view.LevelNameText.text = normalScenarioInfo.ScenarioName;
        _view.LevelDescriptionText.text = normalScenarioInfo.Description;
        _view.BackgroundImage.sprite = normalScenarioInfo.Thumbnail;

        LoadBestRecord(normalScenarioInfo, hardScenarioInfo);
    }

    private void OnLevelSelectButtonClick(ScenarioInfo normalScenarioInfo, ScenarioInfo hardScenarioInfo)
    {
        _normalScenarioToBeLoaded = normalScenarioInfo;
        _hardScenarioToBeLoaded = hardScenarioInfo;
        OpenDifficultySelect();
    }

    private void OpenDifficultySelect()
    {
        _view.DifficultyPopup.gameObject.SetActive(true);
        _focus.OpenPopup(_view.DifficultyPopup, _view.NormalDifficultyButton.Button);
    }

    private void CloseDifficultySelect()
    {
        _view.DifficultyPopup.gameObject.SetActive(false);
        _focus.ClosePopup();
    }

    private void LoadBestRecord(ScenarioInfo normalScenarioInfo, ScenarioInfo hardScenarioInfo)
    {
        SavedLevelProgressData saveData = _progressManager.LevelProgressData;
        LevelProgressInfo normalRecord = saveData.CompletedLevelProgressInfoArray.Where(level => level.LevelAssetsName == normalScenarioInfo.ScenarioAssetsSceneName).FirstOrDefault();
        LevelProgressInfo hardRecord = saveData.CompletedLevelProgressInfoArray.Where(level => level.LevelAssetsName == hardScenarioInfo.ScenarioAssetsSceneName).FirstOrDefault();

        bool isNormalRecordExists = normalRecord != null;
        bool isHardRecordExists = hardRecord != null;

        RemoveObjectiveRows();

        if (normalScenarioInfo != null)
        {
            if (isNormalRecordExists)
            {
                int bestRecord = normalRecord.PersonalBestActionCount;
                string bestRecordFormat = bestRecord == 1 ? BEST_RECORD_SINGULAR_FORMAT : BEST_RECORD_PLURAL_FORMAT;
                _view.NormalBestRecordText.text = string.Format(bestRecordFormat, NORMAL_DIFFICULTY_NAME, bestRecord);
            }
            else
            {
                _view.NormalBestRecordText.text = string.Format(NO_RECORD_FORMAT, NORMAL_DIFFICULTY_NAME);
            }

            for (int i = 0; i < normalScenarioInfo.Objectives.OptionalObjectives.Count; i++)
            {
                OptionalObjective optionalObjective = normalScenarioInfo.Objectives.OptionalObjectives[i];
                bool isObjectiveComplete = isNormalRecordExists && normalRecord.OptionalObjectivesCompletions[i];
                AddObjectiveRow(optionalObjective.Description, isObjectiveComplete, isNormalDifficulty: true);
            }
        }
        else
        {
            // We don't have a scenario for this difficulty
            _view.NormalBestRecordText.text = string.Empty;
        }


        if (hardScenarioInfo != null)
        {
            if (isHardRecordExists)
            {
                int bestRecord = hardRecord.PersonalBestActionCount;
                string bestRecordFormat = bestRecord == 1 ? BEST_RECORD_SINGULAR_FORMAT : BEST_RECORD_PLURAL_FORMAT;
                _view.HardBestRecordText.text = string.Format(bestRecordFormat, HARD_DIFFICULTY_NAME, bestRecord);
            }
            else
            {
                _view.HardBestRecordText.text = string.Format(NO_RECORD_FORMAT, HARD_DIFFICULTY_NAME);
            }

            for (int i = 0; i < hardScenarioInfo.Objectives.OptionalObjectives.Count; i++)
            {
                OptionalObjective optionalObjective = hardScenarioInfo.Objectives.OptionalObjectives[i];
                bool isObjectiveComplete = isHardRecordExists && hardRecord.OptionalObjectivesCompletions[i];
                AddObjectiveRow(optionalObjective.Description, isObjectiveComplete, isNormalDifficulty: false);
            }
        }
        else
        {
            // We don't have a scenario for this difficulty
            _view.HardBestRecordText.text = string.Empty;
        }
    }

    private void AddObjectiveRow(string objectiveName, bool isComplete, bool isNormalDifficulty)
    {
        Transform container = isNormalDifficulty ? _view.NormalOptionalObjectivesContainer.transform : _view.HardOptionalObjectivesContainer.transform;

        LevelSelectObjectiveRowView rowView = Instantiate(_view.OptionalObjectivesPrefab, container);
        rowView.ObjectiveNameText.text = objectiveName;

        rowView.Check.gameObject.SetActive(isComplete);
        rowView.Cross.gameObject.SetActive(!isComplete);

        rowView.gameObject.SetActive(true);
    }

    private void RemoveObjectiveRows()
    {
        Transform normalContainer = _view.NormalOptionalObjectivesContainer.transform;
        Transform hardContainer = _view.HardOptionalObjectivesContainer.transform;

        // Index 0 is the template row so destroy children after index 0.
        for (int i = normalContainer.childCount - 1; i > 0; i--)
        {
            Destroy(normalContainer.GetChild(i).gameObject);
        }

        // Empty all children from hard container.
        for (int i = hardContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(hardContainer.GetChild(i).gameObject);
        }
    }
}
