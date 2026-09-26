using UnityEngine;
using UnityEngine.EventSystems;

public class ResultMenuPresenter : MonoBehaviour
{
    [Header("View")]
    [SerializeField] private ResultMenuView _view;

    [Header("Models")]
    [SerializeField] private ReviewPanelPresenter _reviewPresenter;
    [SerializeField] private GameManager _gameManager;

    [Header("Settings")]
    [SerializeField] private Color _completeColor;
    [SerializeField] private Color _failedColorMain;
    [SerializeField] private Color _failedColorOptional;

    private const string LEVEL_COMPLETE_TEXT = "Complete";
    private const string LEVEL_FAILED_TEXT = "Failed";

    private const string CIVILIANS_OBJECTIVE_TEXT = "Civilians Rescued";
    private const string ALLIES_OBJECTIVE_TEXT = "Allies Saved";
    private const string ENEMIES_OBJECTIVE_TEXT = "Enemies Killed";
    private const string OPTIONAL_OBJECTIVE_COMPLETE_TEXT = "Complete";
    private const string OPTIONAL_OBJECTIVE_MISSED_TEXT = "Missed";
    private const string OPTIONAL_OBJECTIVE_LEVEL_FAILED_TEXT = "Scenario Incomplete";
    private const string OBJECTIVE_COUNT_FORMAT = "{0}/{1}";

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        ScenarioInfo scenarioInfo = _gameManager.ScenarioInfo;

        _view.ReviewButton.Button.onClick.AddListener(OnReviewButtonClicked);
        _view.RewindButton.Button.onClick.AddListener(OnRewindButtonClicked);
        _view.RestartButton.Button.onClick.AddListener(() => AdditiveSceneManager.Instance.ReloadScenario());
        _view.NextButton.Button.onClick.AddListener(() => OnNextButtonClicked(scenarioInfo));
        _view.MainMenuButton.Button.onClick.AddListener(() => AdditiveSceneManager.Instance.LoadMainMenu());

        _view.ReviewButton.OnHover += ChangeHint;
        _view.RewindButton.OnHover += ChangeHint;
        _view.RestartButton.OnHover += ChangeHint;
        _view.NextButton.OnHover += ChangeHint;
        _view.MainMenuButton.OnHover += ChangeHint;

        _reviewPresenter.OnMenuClose += OpenMenu;
        _gameManager.OnLevelComplete += OnLevelComplete;

        // Which buttons are shown depends on the result, so pick the first one that is.
        MenuFocus.Attach(gameObject, _view.NextButton.Button, _view.ReviewButton.Button, _view.RewindButton.Button);

        _view.DescriptionText.text = string.Empty;

        CloseMenu();
    }

    private void OpenMenu()
    {
        gameObject.SetActive(true);
        _view.DescriptionText.text = string.Empty;
    }

    private void CloseMenu()
    {
        gameObject.SetActive(false);
        EventSystem.current.SetSelectedGameObject(null);
    }

    private void ChangeHint(string description)
    {
        _view.DescriptionText.text = description;
    }

    private void OnLevelComplete(LevelResults results)
    {
        OpenMenu();
        DisplayLevelStats(results);
    }

    private void DisplayLevelStats(LevelResults results)
    {
        ScenarioInfo scenarioInfo = _gameManager.ScenarioInfo;
        bool isLevelComplete = _gameManager.LevelWon;

        _view.LevelNameText.text = _gameManager.ScenarioInfo.ScenarioName;
        _view.CompletionText.text = isLevelComplete ? LEVEL_COMPLETE_TEXT : LEVEL_FAILED_TEXT;
        _view.ActionCountText.text = results.ActionsTaken.ToString();

        _view.NewRecord.SetActive(results.IsNewRecord);
        _view.NextButton.gameObject.SetActive(isLevelComplete);

        if (scenarioInfo.NumberOfCivilians > 0)
        {
            bool isComplete = results.CiviliansRescued >= scenarioInfo.NumberOfCivilians;
            Color textColor = isComplete ? _completeColor : _failedColorMain;
            AddObjectiveRow(CIVILIANS_OBJECTIVE_TEXT, string.Format(OBJECTIVE_COUNT_FORMAT, results.CiviliansRescued, scenarioInfo.NumberOfCivilians), textColor);
        }
        if (scenarioInfo.NumberOfAllies > 0)
        {
            bool isComplete = results.AlliesSaved >= scenarioInfo.NumberOfAllies;
            Color textColor = isComplete ? _completeColor : _failedColorMain;
            AddObjectiveRow(ALLIES_OBJECTIVE_TEXT, string.Format(OBJECTIVE_COUNT_FORMAT, results.AlliesSaved, scenarioInfo.NumberOfAllies), textColor);
        }
        if (scenarioInfo.NumberOfEnemies > 0)
        {
            bool isComplete = results.EnemiesKilled >= scenarioInfo.NumberOfEnemies;
            Color textColor = isComplete ? _completeColor : _failedColorMain;
            AddObjectiveRow(ENEMIES_OBJECTIVE_TEXT, string.Format(OBJECTIVE_COUNT_FORMAT, results.EnemiesKilled, scenarioInfo.NumberOfEnemies), textColor);
        }

        for (int i = 0; i < scenarioInfo.Objectives.OptionalObjectives.Count; i++)
        {
            string objective = scenarioInfo.Objectives.OptionalObjectives[i].Description;
            bool isOptionalObjectiveComplete = results.OptionalObjectivesComplete[i];
            string completionText;
            if (isLevelComplete)
            {
                completionText = isOptionalObjectiveComplete ? OPTIONAL_OBJECTIVE_COMPLETE_TEXT : OPTIONAL_OBJECTIVE_MISSED_TEXT;
            }
            else
            {
                completionText = OPTIONAL_OBJECTIVE_LEVEL_FAILED_TEXT;
            }
            Color textColor = isOptionalObjectiveComplete ? _completeColor : _failedColorOptional;
            AddObjectiveRow(objective, completionText, textColor);
        }
    }

    private void AddObjectiveRow(string objectiveName, string objectiveStatus, Color textColor)
    {
        ObjectiveRowView rowView = Instantiate(_view.ObjectiveRowPrefab, _view.ObjectiveRowContainer.transform);
        rowView.ObjectiveNameText.text = objectiveName;
        rowView.ObjectiveStatusText.text = objectiveStatus;
        rowView.ObjectiveNameText.color = textColor;
        rowView.ObjectiveStatusText.color = textColor;
        rowView.gameObject.SetActive(true);
    }

    private void RemoveObjectiveRows()
    {
        Transform container = _view.ObjectiveRowContainer.transform;

        // Index 0 is the template row so destroy children after index 0.
        for (int i = container.childCount - 1; i > 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    private void OnNextButtonClicked(ScenarioInfo scenarioInfo)
    {
        if (scenarioInfo.NextEnvironmentSceneName != string.Empty && scenarioInfo.NextScenarioAssetsSceneName != string.Empty)
        {
            AdditiveSceneManager.Instance.LoadScenario(scenarioInfo.NextEnvironmentSceneName, scenarioInfo.NextScenarioAssetsSceneName);
        }
        else
        {
            AdditiveSceneManager.Instance.LoadCredits();
        }
    }

    private void OnRewindButtonClicked()
    {
        _gameManager.AnyBlockingMenuClosed();
        _gameManager.RewindLevel();
        RemoveObjectiveRows();
        CloseMenu();
    }

    private void OnReviewButtonClicked()
    {
        CloseMenu();
        _reviewPresenter.OpenMenu();
    }
}
