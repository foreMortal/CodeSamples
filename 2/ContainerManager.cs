using Cysharp.Threading.Tasks;
using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class ContainersManager : MonoBehaviour, IContainersManager
{
    [SerializeField] private Transform _handler;
    [SerializeField] private AssetReference _letters2Key, _letters3Key, _letters4Key;
    [SerializeField] private List<LetterContainerParent> _containers = new();
    
    private Vector3 startPos, endPos;
    private float _length;
    private readonly float _separator = 20f;

    public async UniTask ReceiveContainers(string[] containers)
    {
        var letters2 = await Addressables.LoadAssetAsync<GameObject>(_letters2Key).Task.AsUniTask();
        var letters3 = await Addressables.LoadAssetAsync<GameObject>(_letters3Key).Task.AsUniTask();
        var letters4 = await Addressables.LoadAssetAsync<GameObject>(_letters4Key).Task.AsUniTask();

        for (int i = 0; i < containers.Length; i++)
        {
            LetterContainerParent newCon = containers[i].Length switch
            {
                3 => Instantiate(letters3, _handler).GetComponent<LetterContainerParent>(),
                4 => Instantiate(letters4, _handler).GetComponent<LetterContainerParent>(),
                _ => Instantiate(letters2, _handler).GetComponent<LetterContainerParent>(),
            };

            newCon.Initialize(containers[i].ToLowerInvariant(), i);
            _containers.Add(newCon);
        }

        _length = _handler.GetComponent<RectTransform>().sizeDelta.x;
        startPos = _handler.transform.localPosition;

        foreach (var c in _containers)
        {
            c.GetManager(this);
        }
        AllignContainers();
    }

    private void AllignContainers()
    {
        float xPos = 0f;

        for(int i = 0; i < _containers.Count; i++)
        {
            xPos += _separator;
            xPos += _containers[i].RectTransform.sizeDelta.x;
            Vector3 newPos = new Vector2(xPos - _containers[i].RectTransform.sizeDelta.x / 2f, 0f);

            var t = _containers[i].transform;
            var tween = DOTween.To(() => t.localPosition, x => t.localPosition = x, newPos, 0.1f);
        }
        endPos = startPos + new Vector3(xPos + _separator, 0f);
    }

    public void MoveContainers(float range)
    {
        Vector3 newPos = new (_handler.transform.localPosition.x + range, 0f);

        if (newPos.x > startPos.x)
            _handler.transform.localPosition = startPos;
        else if (newPos.x < -endPos.x && -endPos.x < startPos.x)
            _handler.transform.localPosition = -endPos;
        else if(-endPos.x < startPos.x)
            _handler.transform.localPosition = newPos;
    }

    public void ReturnContainerToPool(LetterContainerParent container, bool wasPlaced)
    {
        if(wasPlaced)
            _containers.Add(container);

        container.transform.SetParent(_handler);
        AllignContainers();
        MoveContainers(container.RectTransform.sizeDelta.x / 2f);
    }

    public void RemoveContainerFromPool(LetterContainerParent con)
    {
        _containers.Remove(con);
        AllignContainers();
        MoveContainers(con.RectTransform.sizeDelta.x / 2f);
    }
}