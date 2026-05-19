using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
    private const float FadeStartThreshold = 0.7f;
    private const float FadeRange = 0.3f;

    public float moveDuration = 1.0f;
    public float height = 2.0f;
    public float minHorizontalDistance = 0.5f;
    public float maxHorizontalDistance = 1.5f;
    public float fadeSpeed = 2f;

    private TextMeshPro textMesh;
    private Color textColor;
    private float timer;
    private Vector3 startPos;
    private Vector3 endPos;
    private Camera mainCamera;

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        mainCamera = Camera.main;

        textColor = textMesh.color;
        startPos = transform.position;

        float distance = Random.Range(minHorizontalDistance, maxHorizontalDistance);
        float direction = Random.value < 0.5f ? -1f : 1f;
        float randomX = distance * direction;

        endPos = startPos + new Vector3(randomX, 0, 0);
    }

    private void Update()
    {
        ApplyBillboardRotation();

        timer += Time.deltaTime;
        float clampedT = Mathf.Clamp01(timer / moveDuration);

        transform.position = EvaluatePosition(clampedT);
        transform.localScale = EvaluateScale(clampedT);
        UpdateFade(clampedT);

        if (clampedT >= 1f)
        {
            Destroy(gameObject);
        }
    }

    public void SetDamageText(string damage)
    {
        textMesh.text = damage;
    }

    private void ApplyBillboardRotation()
    {
        if (mainCamera != null)
        {
            transform.rotation = mainCamera.transform.rotation;
        }
    }

    private Vector3 EvaluatePosition(float t)
    {
        float y = Mathf.Sin(Mathf.PI * t) * height;
        Vector3 position = Vector3.Lerp(startPos, endPos, t);
        position.y += y;
        return position;
    }

    private Vector3 EvaluateScale(float t)
    {
        float scale = Mathf.Lerp(0.7f, 1.2f, Mathf.Clamp01(t * 2f));
        return Vector3.one * scale;
    }

    private void UpdateFade(float t)
    {
        if (t <= FadeStartThreshold)
            return;

        textColor.a = Mathf.Lerp(1f, 0f, (t - FadeStartThreshold) / FadeRange);
        textMesh.color = textColor;
    }
}
