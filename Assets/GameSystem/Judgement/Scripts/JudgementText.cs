using TMPro;
using UnityEngine;

public class JudgementText : MonoBehaviour
{
    public float destroyTime = 0.8f;
    public float floatSpeed = 0.2f;
    private TextMeshPro textMesh;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
    }

    public void Setup(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        transform.Translate(Vector3.up * floatSpeed * Time.deltaTime, Space.World);
        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }
}