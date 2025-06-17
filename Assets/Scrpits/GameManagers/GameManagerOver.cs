using TMPro;
using DG.Tweening;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using System.Threading;
using Assets.Scrpits.Location;

/// <summary>
/// Мэнэджер для концовки игры
/// </summary>
public class GameManagerOver : MonoBehaviour
{
    [SerializeField] private GameObject resPanel;
    [SerializeField] private GameObject bossPanel;
    [SerializeField] private GameObject nextLevelPanel;
    [SerializeField] private TMP_Text resultGame;
    [SerializeField] private TMP_Text resultSalary;
    [SerializeField] private TMP_Text resultBonus;
    [SerializeField] private TMP_Text totalCash;
    [SerializeField] private Button buttonRerty;
    [SerializeField] private Button buttonContinue;
    [SerializeField] private EndTurnCamera endturncam;
    [SerializeField] private Image panelImage;
    [SerializeField] private Material burnMaterial;

    public bool isWin = false;
    public bool canContinue = false;
    private float burnProgress = 0f;

    public void OnContinutePressed()
    {
        if (canContinue)
        {
            StartCoroutine(BurnAway(1));
        }
    }

    public void OnRetryPressed()
    {
        if (canContinue)
        {
            StartCoroutine(BurnAway(2));
        }
    }

    public void OnMainMenuPressed()
    {
        if (canContinue)
        {
            StartCoroutine(BurnAway(3));
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

            int currentCoins = PlayerPrefs.GetInt("Coins", 0);
            PlayerPrefs.SetInt("Coins", currentCoins + total);
        }
        else
        {
            resultSalary.text = "";
            resultBonus.text = "";
            totalCash.text = "";
        }

        Sequence sequence = DOTween.Sequence();

        resultGame.transform.localPosition = new Vector2(resultGame.transform.localPosition.x, Screen.height + 200f);

        sequence.Append(resultGame.rectTransform.DOAnchorPos(new Vector2(0, 0), 1.25f, false).SetEase(Ease.OutBounce))
                .AppendInterval(0.2f);

        StartCoroutine(DOTextWaveMovement(resultGame));

        sequence.Append(resultSalary.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
              .AppendInterval(0.2f);

        sequence.Append(resultBonus.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
              .AppendInterval(0.2f);

        sequence.Append(totalCash.transform.DOScale(new Vector3(1f, 1f, 1), 0.1f).SetEase(Ease.OutCubic))
               .AppendInterval(0.2f);

        sequence.Play();

        sequence.OnComplete(() =>
        {
            canContinue = true;
        });
    }

    /// <summary>
    /// Анимация текста - получение урона
    /// </summary>
    /// <param name="text">Кол-во урона</param>
    private IEnumerator DOTextWaveMovement(TMP_Text text)
    {
        TMP_TextInfo textInfo = text.textInfo;
        Vector3[] vertices;

        while (resPanel.activeSelf)
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
                    var offset = new Vector3(0, Mathf.Sin(Time.time * 5f + i * 0.75f) * 5f, 0);
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

    public void GameOver(bool result)
    {
        isWin = result;
        resPanel.gameObject.SetActive(true);
        resultGame.text = result ? "VICTORY!" : "DEFEAT...";
        buttonRerty.gameObject.SetActive(!result);
        buttonContinue.gameObject.SetActive(result);
        GetResult(result);
        if (result == true)
        {
            nextLevelPanel.gameObject.SetActive(true);
            bossPanel.gameObject.SetActive(false);
        }

    }

    private IEnumerator BurnAway(int go)
    {
        yield return StartCoroutine(BurnRoutine());
        switch (go)
        {
            case 1:
                StartCoroutine(endturncam.nextLocation());
                break;
            case 2:
                UnityEngine.SceneManagement.SceneManager.LoadScene("EnemyScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                break;
            case 3:
                UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenuScene", UnityEngine.SceneManagement.LoadSceneMode.Single);
                break;
        }
    }

    private IEnumerator BurnRoutine()
    {
        float duration = 1.5f;
        float elapsed = 0f;

        TMP_Text[] texts = resPanel.GetComponentsInChildren<TMP_Text>(true);

        while (elapsed < duration)
        {
            burnProgress = Mathf.Lerp(1f, 0f, elapsed / duration);
            float alpha = burnProgress;

            panelImage.material.SetFloat("_BurnProgress", burnProgress);

            foreach (var txt in texts)
            {
                var color = txt.color;
                color.a = alpha;
                txt.color = color;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        panelImage.material.SetFloat("_BurnProgress", 1f);
        resPanel.SetActive(false);
        yield return new WaitForSecondsRealtime(0.5f);
    }
}
