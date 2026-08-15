using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeDuration = 0.15f;
    public float shakeMagnitude = 0.15f;

    private Vector3 originalPosition;
    private float shakeTimer;

    void Start()
    {
        originalPosition = transform.localPosition;
    }

    void Update()
    {
        if (shakeTimer > 0f)
        {
            transform.localPosition = originalPosition + Random.insideUnitSphere * shakeMagnitude;
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            shakeTimer = 0f;
            transform.localPosition = originalPosition;
        }
    }

    public void Shake()
    {
        shakeTimer = shakeDuration;
    }
}