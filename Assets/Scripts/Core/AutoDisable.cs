using UnityEngine;

public class AutoDisable : MonoBehaviour
{
    [SerializeField] private float life = 2f;

    private void OnEnable()
    {
        CancelInvoke(nameof(DisableObject));
        Invoke(nameof(DisableObject), life);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(DisableObject));
    }

    private void DisableObject()
    {
        if (ObjectPooler.Instance != null)
        {
            ObjectPooler.Instance.ReturnToPool(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
