using TMPro;
using UnityEngine;
[RequireComponent(typeof(TMP_Text))]
public class RainbowWaveText : MonoBehaviour
{
    public float waveAmplitude = 8f;
    public float waveFrequency = 4f;
    public float rainbowSpeed = 1f;
    public Color lightBlue = new Color(0.6f, 0.85f, 1f);
    public Color darkBlue = new Color(0f, 0.15f, 0.6f);
    TMP_Text text;
    void Awake()
    {
        text = GetComponent<TMP_Text>();
    }
    void Update()
    {
        text.ForceMeshUpdate();
        TMP_TextInfo textInfo = text.textInfo;
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible)
                continue;
            int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertexIndex = textInfo.characterInfo[i].vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;
            Color32[] colors = textInfo.meshInfo[materialIndex].colors32;
            float offset = Mathf.Sin(Time.time * waveFrequency + i * 0.5f) * waveAmplitude;
            float t = (Mathf.Sin(Time.time * rainbowSpeed + i * 0.08f) + 1f) * 0.5f;
            Color blend = Color.Lerp(darkBlue, lightBlue, t);
            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j].y += offset;
                colors[vertexIndex + j] = blend;
            }
        }
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            text.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
        text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }
}