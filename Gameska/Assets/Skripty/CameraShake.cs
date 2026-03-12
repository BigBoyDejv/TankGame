using UnityEngine;
using System.Collections;

public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance;

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool isShaking = false;

    void Awake()
    {
        // Singleton pattern pre jednoduché volanie odkiaľkoľvek
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void Shake(float duration, float magnitude)
    {
        if (isShaking) return;
        StartCoroutine(ShakeCoroutine(duration, magnitude));
    }

    private IEnumerator ShakeCoroutine(float duration, float magnitude)
    {
        isShaking = true;
        originalPosition = transform.localPosition;
        originalRotation = transform.localRotation;
        
        float elapsed = 0.0f;

        while (elapsed < duration)
        {
            // Menšie trasenie pre rotáciu, väčšie pre pozíciu
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            
            transform.localPosition = new Vector3(originalPosition.x + x, originalPosition.y + y, originalPosition.z);
            transform.localRotation = originalRotation * Quaternion.Euler(y * 2f, x * 2f, 0f);

            elapsed += Time.deltaTime;

            yield return null;
        }

        transform.localPosition = originalPosition;
        transform.localRotation = originalRotation;
        isShaking = false;
    }
}
