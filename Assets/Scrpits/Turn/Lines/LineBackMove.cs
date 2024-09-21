using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Класс для проверки спорикосается ли <see cref="LineBackMove"/> с другими обьектами
/// </summary>
public class LineBackMove : MonoBehaviour
{
    [SerializeField] private float delayCam;
    public List<MoveForward> moveBackLines;
    private MoveActivation moveActivation;

    private void Start()
    {
        moveActivation = new MoveActivation();
    }
    public IEnumerator moveBackCards()
    {
        yield return StartCoroutine(moveActivation.moveCards(moveBackLines, 0.25f));
        yield return new WaitForSeconds(delayCam);
    }
    public void OnTriggerEnter(Collider hit)
    {
        if (hit.tag == "Enemy")
        {
            var moveForward = hit.gameObject.GetComponentInParent<MoveForward>();

            if (moveForward != null && moveForward.isMovingBackLine == true)
            {
                moveBackLines.Add(moveForward);
            }
        }
    }
}
