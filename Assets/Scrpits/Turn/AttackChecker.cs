using System.Collections;
using Assets.Utility;
using UnityEngine;

public class AttackChecker : MonoBehaviour
{
    public GameObject[] lines;

    private RectTransform rectTransform;
    private EndTurnCamera turnCamera;
    private MeshRenderer mesh;
    private Collider coll;

    private void Awake()
    {
        turnCamera = FindObjectOfType<EndTurnCamera>().GetComponent<EndTurnCamera>();
        rectTransform = GetComponent<RectTransform>();
        mesh = GetComponent<MeshRenderer>();
        coll = GetComponent<Collider>();
    }
    public IEnumerator changeLine()
    {
        mesh.enabled = true;
        coll.enabled = true;
        for (var index = 0; index < lines.Length; index++)
        {
            transform.SetParent(lines[index].transform);
            rectTransform.anchoredPosition = new Vector2(
                0,
                0
            );
            yield return new WaitForSeconds(1);
        }
        mesh.enabled = false;
        coll.enabled = false;
        StartCoroutine(turnCamera.ChangeRotation());
    }
    public void OnTriggerEnter(Collider hit)
    {
        if (hit.CompareTag("Card"))
        {
            var cardAttack = hit.gameObject.GetComponentInParent<CardAttack>();
            DebugUtility.HandleErrorIfNullGetComponent<CardAttack, AttackChecker>(cardAttack, this, gameObject);
            if (cardAttack != null)
            {
                cardAttack.PerformAttack();
            }
        }
    }
}
