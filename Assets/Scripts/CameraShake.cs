using Unity.Cinemachine;
using UnityEngine;

public class CameraShake : MonoBehaviour
{
    [Header("Shake Settings")]
    public float shakeAmplitude = 0.5f;
    public float shakeFrequency = 2f;
    public float shakeDuration = 0.15f;

    private CinemachineImpulseSource impulseSource;

    void Start()
    {
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void Shake()
    {
        if (impulseSource != null)
        {
            impulseSource.GenerateImpulse(Vector3.down * shakeAmplitude);
        }
    }
}