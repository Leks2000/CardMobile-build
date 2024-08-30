using TMPro;
using DG.Tweening;
using UnityEngine;
using System.Linq;
using System.Collections;

public class GameManagerOver : MonoBehaviour
{
    [SerializeField] private GameObject resPanel;
    [SerializeField] private TMP_Text resultGame;
    [SerializeField] private TMP_Text resultSalary;
    [SerializeField] private TMP_Text resultBonus;
    [SerializeField] private TMP_Text totalCash;
    [SerializeField] private TMP_Text cashOUT;

    private void GetResult()
    {
        resultSalary.text = "Salary" + new string(' ', 35) + "26$";
        resultBonus.text = "No Damage Bonus" + new string(' ', 12) + "14$";
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
    }

    private IEnumerator DOTextWaveMovement(TMP_Text text, float duration)
    {
        TMP_TextInfo textInfo = text.textInfo;
        Vector3[] vertices;

        for (float time = 0; time < duration; time += Time.deltaTime)
        {
            text.ForceMeshUpdate();
            textInfo = text.textInfo;

            for (int i = 0; i < textInfo.characterCount; i++)
            {
                if (!textInfo.characterInfo[i].isVisible)
                {
                    continue;
                }

                vertices = textInfo.meshInfo[textInfo.characterInfo[i].materialReferenceIndex].vertices;

                for (int j = 0; j < 4; j++)
                {
                    Vector3 offset = new Vector3(0, Mathf.Sin(time * 5f + i * 0.75f) * 5f, 0);
                    vertices[textInfo.characterInfo[i].vertexIndex + j] += offset;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return null;
        }
    }
    public void GameOver()
    {
        resPanel.gameObject.SetActive(true);
        GetResult();
    }
}
