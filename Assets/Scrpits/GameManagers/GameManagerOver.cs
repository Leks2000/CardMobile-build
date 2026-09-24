using TMPro;
using DG.Tweening;
using UnityEngine;
using System.Linq;
using System.Collections;
using UnityEngine.UI;
using System.Threading;
using Assets.Scrpits.Location;
using Assets.Scrpits.Run;

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
    [SerializeField] private EndTurnCamera endturncam;

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
    /// [V] Результат боя: награда (BattleRewards.Grant ровно один раз при победе) + оверлей ResultOverlayView.
    /// Старый блок "Salary / Bonus / PlayerPrefs" удалён - единая валюта теперь Wallet.
    /// </summary>
    private void GetResult(bool result)
    {
        if (result)
        {
            BattleRewards.Grant();
        }

        buttonNextScene.interactable = false;
        var view = GetComponent<ResultOverlayView>();
        if (view == null) view = gameObject.AddComponent<ResultOverlayView>();
        view.Play(result, resPanel, resultGame, new[] { resultSalary, resultBonus, totalCash, cashOUT }, () =>
        {
            canContinue = true;
            buttonNextScene.interactable = true;
        });
        StartCoroutine(DOTextWaveMovement(resultGame, 25f));
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
        // Экран результата плавно гаснет вместе с затемнением экрана (без «сжимающейся» чёрной панели)
        var group = resPanel.GetComponent<CanvasGroup>();
        if (group == null) group = resPanel.AddComponent<CanvasGroup>();
        group.interactable = false;
        group.DOFade(0f, 0.35f).SetUpdate(true);

        // Без забега (EnemyScene запущена напрямую) и победа - старое поведение (двери следующей локации)
        if (!RunState.IsActive && isWin)
        {
            yield return new WaitForSeconds(0.35f);
            resPanel.SetActive(false);
            yield return StartCoroutine(endturncam.nextLocation());
            yield break;
        }

        bool done = false;
        SceneFade.Out(0.4f, () => done = true);
        while (!done) yield return null;

        // Забег: результат боя -> обратно на карту (победа над боссом / поражение показываются там)
        if (RunState.IsActive)
        {
            if (isWin)
            {
                RunBattleSetup.StorePlayerHp();
                RunState.CompleteCurrentNode();
                RunState.PendingBattleReward = true; // [M] карта покажет тост BattleRewards.Last*
            }
            else
            {
                RunState.FailRun();
            }
            RunState.LoadMap();
            yield break;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("EnemyScene");
    }


    public void GameOver(bool result)
    {
        isWin = result;
        resPanel.gameObject.SetActive(true);
        resultGame.text = result ? "VICTORY!" : "DEFEAT...";
        GetResult(result);
    }
}
