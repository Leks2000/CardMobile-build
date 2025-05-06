using TMPro;
using DG.Tweening;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;

/// <summary>
/// Мэнэджер для концовки игры
/// </summary>
public class GameManagerOver : MonoBehaviour
{
    [SerializeField] private GameObject resPanel;
    [SerializeField] private TMP_Text resultGame;
    [SerializeField] private TMP_Text resultSalary;
    [SerializeField] private TMP_Text resultBonus;
    [SerializeField] private TMP_Text totalCash;
    [SerializeField] private TMP_Text cashOUT;
    [SerializeField] private Button buttonNextScene;

    private bool isWin = false;
    private bool canContinue = false;

    public void OnTapToContinue()
    {
        if (canContinue)
        {
            StartCoroutine(HandleTapToContinue());
            canContinue = false;
        }
    }

    /// <summary>
    /// Временная система наград
    /// </summary>
    private void GetResult(bool result)
    {
        if (result)
        {
            int salary = 26;
            int bonus = 14;
            int total = salary + bonus;

            resultSalary.text = "Salary" + new string(' ', 35) + $"{salary}$";
            resultBonus.text = "No Damage Bonus" + new string(' ', 12) + $"{bonus}$";
            totalCash.text = "Total" + new string(' ', 38) + $"{total}$";
            cashOUT.text = "CASH OUT";

            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", currentCoins + total);
        }
        else
        {
            resultSalary.text = "";
            resultBonus.text = "";
            totalCash.text = "";
            cashOUT.text = "TAP TO CONTINUE";
        }

        Sequence sequence = DOTween.Sequence();

        resultGame.transform.localPosition = new Vector2(resultGame.transform.localPosition.x, Screen.height + 200f);

        sequence.Append(resultGame.rectTransform.DOAnchorPos(new Vector2(0, 0), 1.25f, false).SetEase(Ease.OutBounce))
                .AppendInterval(0.2f);

        StartCoroutine(DOTextWaveMovement(resultGame, 25f));

        sequence.Append(resultSalary.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
              .AppendInterval(0.2f);

        sequence.Append(resultBonus.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
              .AppendInterval(0.2f);

        sequence.Append(totalCash.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
               .AppendInterval(0.2f);

        sequence.Append(cashOUT.transform.DOScale(new Vector3(1f, 1f, 1), 1f).SetEase(Ease.OutElastic));

        sequence.Append(cashOUT.transform.DOScale(new Vector3(1f, 1f, 1), 1.5f).SetEase(Ease.OutElastic));
        sequence.Play();

        buttonNextScene.interactable = false;
        sequence.OnComplete(() =>
        {
            canContinue = true;
            buttonNextScene.interactable = true;
        });
    }

    /// <summary>
    /// Анимация текста - получение урона
    /// </summary>
    /// <param name="text">Кол-во урона</param>
    private IEnumerator DOTextWaveMovement(TMP_Text text, float duration)
    {
        TMP_TextInfo textInfo = text.textInfo;
        Vector3[] vertices;

        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            text.ForceMeshUpdate();
            textInfo = text.textInfo;

            for (var i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                vertices = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex].vertices;

                for (var j = 0; j < 4; j++)
                {
                    var offset = new Vector3(0, Mathf.Sin(time * 5f + i * 0.75f) * 5f, 0);
                    vertices[textInfo.characterInfo[i].vertexIndex + j] += offset;
                }
            }

            for (var i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }

    private IEnumerator HandleTapToContinue()
    {
        Sequence exitSeq = DOTween.Sequence();
        exitSeq.Append(resPanel.transform.DOScale(0f, 0.5f).SetEase(Ease.InBack));
        yield return exitSeq.WaitForCompletion();

        if (isWin)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        }
    }


    public void GameOver(bool result)
    {
        isWin = result;

        resPanel.gameObject.SetActive(true);
        resultGame.text = result ? "VICTORY!" : "DEFEAT...";
        GetResult(result);
    }
}
