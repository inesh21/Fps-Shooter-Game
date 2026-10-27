using System.Collections;
using UnityEngine;

public class GameFeel : MonoBehaviour
{
    public static GameFeel Instance { get; private set; }

    [Header("FOV Dynamics")]
    [SerializeField] private Camera targetCamera;
    [SerializeField] private float baseFov = 75f;
    [SerializeField] private float sprintFov = 85f;
    [SerializeField] private float fovLerpSpeed = 8f;

    [Header("Camera Roll / Strafe Tilt")]
    [SerializeField] private Transform tiltRoot;
    [SerializeField] private float strafeTilt = 2f;
    [SerializeField] private float tiltLerpSpeed = 8f;

    [Header("Hit Marker")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hitMarkerSound;
    [SerializeField] private AudioClip headshotSound;

    public bool IsSprinting { get; set; }
    public bool IsAiming { get; set; }
    public float StrafeInput { get; set; }

    public delegate void HitMarkerDelegate(bool headshot);
    public event HitMarkerDelegate OnHitMarkerTriggered;

    private float currentTilt;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        if (targetCamera == null)
            targetCamera = GetComponentInChildren<Camera>();

        if (tiltRoot == null && targetCamera != null)
            tiltRoot = targetCamera.transform;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    private void LateUpdate()
    {
        if (targetCamera != null && !IsAiming)
        {
            float targetFov = IsSprinting ? sprintFov : baseFov;
            targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFov, Time.deltaTime * fovLerpSpeed);
        }

        if (tiltRoot != null)
        {
            currentTilt = Mathf.Lerp(currentTilt, -StrafeInput * strafeTilt, Time.deltaTime * tiltLerpSpeed);
            Vector3 euler = tiltRoot.localEulerAngles;
            tiltRoot.localRotation = Quaternion.Euler(euler.x, euler.y, currentTilt);
        }
    }

    public void TriggerHitMarker(bool headshot)
    {
        OnHitMarkerTriggered?.Invoke(headshot);

        if (audioSource != null)
        {
            AudioClip clip = headshot && headshotSound != null ? headshotSound : hitMarkerSound;
            if (clip != null)
            {
                audioSource.pitch = Random.Range(0.95f, 1.05f);
                audioSource.PlayOneShot(clip, 0.7f);
            }
        }
    }

    public void HitStop(float seconds = 0.05f)
    {
        StartCoroutine(HitStopRoutine(seconds));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1f;
    }
}
