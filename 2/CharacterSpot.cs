using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSpot : MonoBehaviour, ICharacterSpot
{
    private LetterContainerParent _placedContainer;
    private Image _img;
    private Tweener _tween;
    public bool IsFree { get; private set; }

    public void Awake()
    {
        IsFree = true;
        _img = GetComponent<Image>();
    }

    private void OnDestroy() => _tween?.Kill();

    public void WordGuessed()
    {
        _placedContainer.TurnOff();
    }

    public void ShowLetterValidation(Color from, Color to)
    {
        _img.color = from;
        if (_tween != null && _tween.active)
            _tween.Kill();

        _tween = DOTween.To(() => _img.color, x => _img.color = x, to, 3f);
    }

    public void PlaceContainer(LetterContainerParent con)
    {
        _placedContainer = con;
        _placedContainer.ContainerGrabbed += Free;
        IsFree = false;
    }
    public bool TryGetContainerInfo(out LetterContainerParent con)
    {
        if (IsFree)
            con = null;
        else
            con = _placedContainer;

        return !IsFree;
    }
    private void Free(LetterContainerParent con)
    {
        IsFree = true;
        con.ContainerGrabbed -= Free;
        _placedContainer = null;
    }
}