using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CustomButtonView : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    ISelectHandler, IDeselectHandler,
    IPointerDownHandler, ISubmitHandler
{
    public Action<string> OnHover;

    [field: Header("Components")]
    [field: SerializeField] public Button Button { get; private set; }
    [field: SerializeField] protected TMP_Text _text;

    [field: Header("Settings")]
    [field: SerializeField] private string _hint;
    [field: SerializeField] protected Color _defaultColor;
    [field: SerializeField] protected Color _highlightColor;

    public virtual void OnPointerEnter(PointerEventData eventData)
    {
        _text.color = _highlightColor;
        OnHover?.Invoke(_hint);
    }

    public virtual void OnPointerExit(PointerEventData eventData)
    {
        _text.color = _defaultColor;
    }

    public virtual void OnSelect(BaseEventData eventData)
    {
        _text.color = _highlightColor;
        OnHover?.Invoke(_hint);
    }

    public virtual void OnDeselect(BaseEventData eventData)
    {
        _text.color = _defaultColor;
    }

    // Fire the click sound on pointer-down and submit. This runs via the
    // EventSystem pipeline independently of Button.onClick listeners, so the
    // sound plays even when the click handler deactivates the button's panel
    // or unloads the scene before onClick listeners finish invoking.
    public virtual void OnPointerDown(PointerEventData eventData)
    {
        if (!IsInteractable()) return;
        PlayClickSound();
    }

    public virtual void OnSubmit(BaseEventData eventData)
    {
        if (!IsInteractable()) return;
        PlayClickSound();
    }

    private bool IsInteractable()
    {
        return Button == null || Button.IsInteractable();
    }

    private void PlayClickSound()
    {
        if (SFXPlayer.Instance != null)
        {
            SFXPlayer.Instance.Play(SfxId.UIClick);
        }
    }
}
