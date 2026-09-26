using System;
using UnityEngine;

public class ReviewPanelPresenter : MonoBehaviour
{
    public Action OnMenuClose;

    [Header("View")]
    [SerializeField] private ReviewPanelView _view;

    [Header("Models")]
    [SerializeField] private GameManager _gameManager;
    [SerializeField] private ReplayCameraManager _replayCameraManager;

    private const string CAMERA_NUMBER_FORMAT = "{0}/{1}";
    private int _numberOfCameras;

    private PlayerActions _inputActions;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _view.gameObject.SetActive(false);

        if (_gameManager.ScenarioInfo != null)
        {
            DisplayScenarioInfo(_gameManager.ScenarioInfo);
        }

        _view.PreviousButton.Button.onClick.AddListener(OnPrevPressed);
        _view.NextButton.Button.onClick.AddListener(OnNextPressed);
        _view.BackButton.Button.onClick.AddListener(OnBackPressed);

        _replayCameraManager.OnReplayStart += OnReplayStart;
        _replayCameraManager.OnCameraChange += OnCameraChange;

        MenuFocus.Attach(_view.gameObject, _view.NextButton.Button, _view.BackButton.Button);

        _inputActions = new PlayerActions();
        _inputActions.UI.Cancel.performed += _ => CloseMenu();
    }

    public void OpenMenu()
    {
        _view.gameObject.SetActive(true);
        _inputActions.UI.Enable();
    }

    public void CloseMenu()
    {
        _view.gameObject.SetActive(false);
        _inputActions.UI.Disable();
        OnMenuClose?.Invoke();
    }

    private void DisplayScenarioInfo(ScenarioInfo scenarioInfo)
    {
        _view.LevelNameText.text = scenarioInfo.ScenarioName;
    }

    private void OnReplayStart(int numberOfCameras)
    {
        _numberOfCameras = numberOfCameras;
        _view.CurrentCameraText.text = string.Format(CAMERA_NUMBER_FORMAT, 1, _numberOfCameras);
    }

    private void OnCameraChange(int cameraIndex)
    {
        _view.CurrentCameraText.text = string.Format(CAMERA_NUMBER_FORMAT, cameraIndex, _numberOfCameras);
    }

    private void OnPrevPressed()
    {
        _replayCameraManager.CycleCameraPrevious();
    }

    private void OnNextPressed()
    {
        _replayCameraManager.CycleCameraNext();
    }

    private void OnBackPressed()
    {
        CloseMenu();
    }
}
