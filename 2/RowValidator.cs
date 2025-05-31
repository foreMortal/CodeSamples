using System.Text;
using UnityEngine;
using Zenject;

public class RowValidator : MonoBehaviour, IRowValidator
{
    [SerializeField] private ICharacterSpot[] _spots;
    
    private IWordsManager _manager;
    private bool _isGuessed;

    [Inject]
    public void Contruct(IWordsManager m)
    {
        _manager = m;
    }

    private void Start()
    {
        _spots = GetComponentsInChildren<ICharacterSpot>();
    }

    public void ValidateRow(Color good, Color bad, Color neutral)
    {
        if (!_isGuessed)
        {
            var word = CombineAWord();

            var closest = FindClosetWord(word);

            bool correct = true;
            for (int i = 0; i < closest.Length; i++)
            {
                if (word[i] == closest[i])
                {
                    _spots[i].ShowLetterValidation(good, neutral);
                    continue;
                }
                else
                {
                    Color col = word[i] switch
                    {
                        ' ' => neutral,
                        _ => bad,
                    };

                    _spots[i].ShowLetterValidation(col, neutral);
                    correct = false;
                }
            }

            if (correct)
            {
                _manager.WordGuessed(word);
                foreach (var s in _spots)
                    s.WordGuessed();
                _isGuessed = true;
            }
        }
    }

    private string FindClosetWord(string word)
    {
        int maxCorrectCount = 0;
        var mostCloseWord = "000000";

        for (int i = 0; i < _manager.WordsToGuess.Count; i++)
        {
            int correctCount = 0;
            for (int j = 0; j < _manager.WordsToGuess[i].Length; j++)
            {
                if (word[j] == _manager.WordsToGuess[i][j])
                    correctCount++;
            }

            if (correctCount > maxCorrectCount)
            {
                mostCloseWord = _manager.WordsToGuess[i];
                maxCorrectCount = correctCount;
            }
        }

        return mostCloseWord;
    }
    private string CombineAWord()
    {
        int lastIndex = -1;
        var sb = new StringBuilder();

        for (int i = 0; i < _spots.Length; i++)
        {
            bool haveCon = _spots[i].TryGetContainerInfo(out LetterContainerParent con);

            if (!haveCon)
            {
                sb.Append(" ");
                continue;
            }
            else if (lastIndex != con.Index)
            {
                sb.Append(con.Letters);
                lastIndex = con.Index;
            }
        }

        var s = sb.ToString();
        sb.Clear();
        return s.ToLower();
    }
}