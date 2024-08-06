using System.Collections;
using Unity.VisualScripting;
using UnityEngine;

public class AttackChecker : MonoBehaviour
{
    public GameObject[] lines;

    EndTurnCamera turnCamera;
    RectTransform rectTransform;
    MeshRenderer mesh;
    Collider coll;

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
        for (int i = 0; i < lines.Length; i++)
        {
            transform.SetParent(lines[i].transform);
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
            CardAttack cardAttack = hit.gameObject.GetComponentInParent<CardAttack>();
            if (cardAttack != null)
            {
                cardAttack.PerformAttack();
            }
        }
    }
}
