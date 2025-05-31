using DG.Tweening;
using System;
using UnityEngine;
using UnityEngine.UI;

public class LetterContainer : LetterContainerParent
{
    [SerializeField] Image[] _visualIndicators;
    private PlaySoundByID _soundPlayer;
    private bool _wasPlaced;
    public override Image[] VisualIndicators { get { return _visualIndicators; } }

    public override event Action<LetterContainerParent> ContainerGrabbed;

    private void Awake()
    {
        _soundPlayer = GetComponent<PlaySoundByID>();
    }

    public override void Place(Vector3 pos)
    {
        _soundPlayer.PlayById(1);
        _wasPlaced = true;

        foreach (var vis in _visualIndicators)
            vis.gameObject.SetActive(false);

        _manager.RemoveContainerFromPool(this);

        Vector3 oldPos = transform.position;
        DOTween.To(() => oldPos, x => transform.position = oldPos = x, pos, 0.1f);
    }

    public override void Return()
    {
        _soundPlayer.PlayById(2);
        _manager.ReturnContainerToPool(this, _wasPlaced);
        _wasPlaced = false;

        foreach (var vis in _visualIndicators)
            vis.gameObject.SetActive(false);
    }

    public override void Move(Vector3 newPosition)
    {
        transform.position = newPosition;
    }

    public override void Grab(Transform newParent)
    {
        _soundPlayer.PlayById(0);
        ContainerGrabbed?.Invoke(this);
        transform.SetParent(newParent);

        foreach(var vis in _visualIndicators)
            vis.gameObject.SetActive(true);
    }

    public override void TurnOff()
    {
        gameObject.layer = 0;
    }
}