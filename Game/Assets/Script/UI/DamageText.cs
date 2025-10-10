using UnityEngine;
using TMPro;

public class DamageText : MonoBehaviour
{
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

    private void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();

        textColor = textMesh.color;
        startPos = transform.position;

        float distance = Random.Range(minHorizontalDistance, maxHorizontalDistance);
        float direction = Random.value < 0.5f ? -1f : 1f;
        float randomX = distance * direction;

        endPos = startPos + new Vector3(randomX, 0, 0);
    }

    private void Update()
    {
        timer += Time.deltaTime;
        float t = timer / moveDuration;

        float y = Mathf.Sin(Mathf.PI * t) * height;
        Vector3 pos = Vector3.Lerp(startPos, endPos, t) + Vector3.up * y;
        transform.position = pos;

        float scale = Mathf.Lerp(0.7f, 1.2f, Mathf.Clamp01(t * 2));
        transform.localScale = Vector3.one * scale;

        if (t > 0.7f)
        {
            textColor.a = Mathf.Lerp(1f, 0f, (t - 0.7f) / 0.3f);
            textMesh.color = textColor;
        }

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    public void SetDamageText(string damage)
    {
        textMesh.text = damage;
    }
}
