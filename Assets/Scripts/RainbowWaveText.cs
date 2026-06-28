using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class RainbowWaveText : MonoBehaviour
{
    public float waveAmplitude = 8f;
    public float waveFrequency = 4f;
    public float rainbowSpeed = 1f;

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

            for (int j = 0; j < 4; j++)
            {
                vertices[vertexIndex + j].y += offset;

                colors[vertexIndex + j] = Color.HSVToRGB(
                    Mathf.Repeat(Time.time * rainbowSpeed + i * 0.08f, 1f),
                    1f,
                    1f);
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