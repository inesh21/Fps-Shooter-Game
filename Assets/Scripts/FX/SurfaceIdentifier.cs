using UnityEngine;

public class SurfaceIdentifier : MonoBehaviour
{
    [SerializeField] private SurfaceType surface = SurfaceType.Default;

    public SurfaceType Surface => surface;
}
