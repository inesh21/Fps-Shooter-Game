using System.Collections;
using UnityEngine;

public class BulletTracer : MonoBehaviour
{
    [SerializeField] private float speed = 100f;
    private Vector3 targetPosition;

    public void Init(Vector3 start, Vector3 target)
    {
        transform.position = start;
        targetPosition = target;
        StopAllCoroutines();
        StartCoroutine(MoveToTarget());
    }

    private IEnumerator MoveToTarget()
    {
        while (Vector3.Distance(transform.position, targetPosition) > 0.1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPosition, speed * Time.deltaTime);
            yield return null;
        }

        transform.position = targetPosition;
        gameObject.SetActive(false); // Return to object pool
    }
}