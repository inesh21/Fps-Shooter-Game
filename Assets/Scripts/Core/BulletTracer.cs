using UnityEngine;

public sealed class BulletTracer : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float speed = 100f;
    private Vector3 destination;
    private bool travelling;

    public void Init(Vector3 start, Vector3 target)
    {
        transform.position = start;
        destination = target;
        travelling = (target - start).sqrMagnitude > 0.0001f;
        if (!travelling) Return();
    }

    private void Update()
    {
        if (!travelling) return;
        transform.position = Vector3.MoveTowards(transform.position, destination, speed * Time.deltaTime);
        if ((transform.position - destination).sqrMagnitude <= 0.0001f)
        {
            transform.position = destination;
            Return();
        }
    }

    private void Return()
    {
        travelling = false;
        if (ObjectPooler.Instance != null) ObjectPooler.Instance.ReturnToPool(gameObject);
        else gameObject.SetActive(false);
    }
}
