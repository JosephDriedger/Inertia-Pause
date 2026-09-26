using System;
using System.Collections;
using UnityEngine;

public class CreditsPresenter : MonoBehaviour
{
    public Action OnCreditsComplete;
    public Action OnMenuClose;

    [Header("View")]
    [SerializeField] private CreditsView _view;

    [Header("Settings")]
    [SerializeField] private float _showSlideDuration = 2f;
    [SerializeField] private float _slideOneWayFadeDuration = 0.5f;

    private Coroutine _slideshowCoroutine;
    private WaitForSeconds _slideDelay;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (_view.BackButton != null)
        {
            _view.BackButton.Button.onClick.AddListener(CloseMenu);
            MenuFocus.Attach(_view.gameObject, _view.BackButton.Button);
        }

        _slideDelay = new (_showSlideDuration);
    }

    public void OpenMenu()
    {
        foreach (CanvasGroup slide in _view.Slides)
        {
            slide.alpha = 0f;
        }

        _view.gameObject.SetActive(true);
    }

    public void CloseMenu()
    {
        OnMenuClose?.Invoke();
        _view.gameObject.SetActive(false);
    }

    public void RunSlideshow()
    {
        if (_slideshowCoroutine != null)
        {
            StopCoroutine(_slideshowCoroutine);
        }

        OpenMenu();
        _slideshowCoroutine = StartCoroutine(Slideshow());
    }

    private IEnumerator Slideshow()
    {
        for (int i = 0; i < _view.Slides.Count; i++)
        {
            CanvasGroup currentSlide = _view.Slides[i];

            yield return CrossfadeSlide(currentSlide, isFadeIn: true);
            yield return _slideDelay;
            yield return CrossfadeSlide(currentSlide, isFadeIn: false);
        }

        OnCreditsComplete?.Invoke();
    }

    private IEnumerator CrossfadeSlide(CanvasGroup slide, bool isFadeIn)
    {
        float startingAlpha = isFadeIn ? 0f : 1f;
        float endingAlpha = isFadeIn ? 1f : 0f;
        float time = 0f;

        slide.alpha = startingAlpha;

        while (time < _slideOneWayFadeDuration)
        {
            time += Time.deltaTime;
            slide.alpha = Mathf.Lerp(startingAlpha, endingAlpha, time / _slideOneWayFadeDuration);
            yield return null;
        }

        slide.alpha = endingAlpha;
    }
}
