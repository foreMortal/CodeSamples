using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

public class WordsManager : MonoBehaviour, IWordsManager
{
    [SerializeField] private RowValidator[] _validators;
    [SerializeField] private List<string> _wordsToGuess;
    [SerializeField] private List<string> _guessedWords;

    private IVicatoryManager _victoryManager;
    private IContainersManager _conManager;
    private Color _good = new(0f, 183f/255f, 0f, 1f), _bad = new(183f/255f, 0f, 0f, 1f), _neutral = new(183f/255f, 183f/255f, 183f/255f, 1f);
    private readonly int maxWordsCount = 4;

    public List<string> WordsToGuess { get { return _wordsToGuess; } }

    [Inject]
    public void Construct(IContainersManager m, IVicatoryManager vm)
    {
        _conManager = m;
        _victoryManager = vm;
    }

    private async UniTask Start()
    {
        var info = await LevelsManager.LoadLevelInfo();

        for(int i = 0; i < maxWordsCount; i++)
        {
            _wordsToGuess.Add(info.wordsToGuess[i].ToLower());
        }

        await _conManager.ReceiveContainers(info.letterContainers);
    }

    public void WordGuessed(string word)
    {
        _wordsToGuess.Remove(word);
        _guessedWords.Add(word);

        if(_wordsToGuess.Count <= 0)
        {
            _victoryManager.OpenVictoryMenu(_guessedWords);
            LevelsManager.LevelWon();
        }
    }

    public void Validate()
    {
        foreach (var v in _validators)
            v.ValidateRow(_good, _bad, _neutral);
    }
}