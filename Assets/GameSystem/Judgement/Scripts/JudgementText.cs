using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class JudgementText : MonoBehaviour
{
    private static List<JudgementText> activeTexts = new List<JudgementText>();

    public float destroyTime = 0.8f;
    public float floatSpeed = 0.2f;
    public float pushUpDistance = 0.35f;
    public float lerpSpeed = 10f;

    private TextMeshPro textMesh;
    private float targetY;

    void Awake()
    {
        textMesh = GetComponent<TextMeshPro>();
        targetY = transform.position.y;
    }

    public void Setup(string text, Color color)
    {
        textMesh.text = text;
        textMesh.color = color;
        
        PushExistingTextsUp();
        
        activeTexts.Add(this);
        Destroy(gameObject, destroyTime);
    }

    private void PushExistingTextsUp()
    {
        for (int i = activeTexts.Count - 1; i >= 0; i--)
        {
            if (activeTexts[i] != null)
            {
                activeTexts[i].targetY += pushUpDistance;
            }
        }
    }

    void Update()
    {
        // Apply continuous slight float upwards
        targetY += floatSpeed * Time.deltaTime;

        // Smoothly interpolate current Y position to targetY
        Vector3 pos = transform.position;
        pos.y = Mathf.Lerp(pos.y, targetY, Time.deltaTime * lerpSpeed);
        transform.position = pos;

        if (Camera.main != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - Camera.main.transform.position);
        }
    }

    private void OnDestroy()
    {
        activeTexts.Remove(this);
    }
}