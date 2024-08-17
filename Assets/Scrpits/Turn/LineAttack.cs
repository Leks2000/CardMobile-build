using System.Collections;
using System.Collections.Generic;
using Assets.Utility;
using UnityEngine;

public class LineAttack : MonoBehaviour
{
    public GameObject[] lines;

    private RectTransform rectTransform;
    private EndTurnCamera turnCamera;
    private MeshRenderer mesh;
    private Collider coll;
    public static List<string> Tags = new List<string>() { "Card", "Enemy" };

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
        rectTransform.localPosition = new Vector3(0, 0, 5);
        for (var index = 0; index < lines.Length; index++)
        {
            transform.SetParent(lines[index].transform);
            rectTransform.anchoredPosition = Vector2.zero;
            yield return new WaitForSeconds(1);
        }
        mesh.enabled = false;
        coll.enabled = false;
        StartCoroutine(turnCamera.ChangeRotation());
    }
    public void OnTriggerEnter(Collider hit)
    {
        if (Tags.Contains(hit.tag))
        {
            var cardForwardAttack = hit.gameObject.GetComponentInParent<CardForwardAttack>();

            cardForwardAttack.HandleErrorIfNullGetComponent<CardForwardAttack, LineAttack>(this, gameObject);
            if (cardForwardAttack != null)
            {
                cardForwardAttack.PerformAttack();
            }
            var moveForward = cardForwardAttack.GetComponentInParent<MoveForward>();
            //moveForward.HandleErrorIfNullGetComponent<MoveForward, LineAttack>(this, gameObject);
            if (moveForward != null)
            {
                turnCamera.moveForwards.Add(moveForward);
                Debug.Log($"{moveForward}");
            }
        }
    }
}
