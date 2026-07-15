using UnityEngine;
using TMPro;

[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public class CurvedTextUGUI : MonoBehaviour
{
    [Header("Curve Settings")]
    [Tooltip("Positive curves text upward (smile), negative curves downward (frown).")]
    [SerializeField, Range(-100f, 100f)] private float curvature = 30f;

    [Tooltip("If true, characters also rotate to follow the curve tangent.")]
    [SerializeField] private bool rotateCharacters = true;

    [Header("Update Behavior")]
    [SerializeField] private bool updateEveryFrame = false;

    private TMP_Text textComponent;
    private readonly Vector3[] worldVertices = new Vector3[4];
    private bool isApplyingCurve;

    private void OnEnable()
    {
        textComponent = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(OnTextChanged);
        ApplyCurve();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(OnTextChanged);
    }

    private void OnValidate()
    {
        if (textComponent == null)
            textComponent = GetComponent<TMP_Text>();

        if (!isApplyingCurve)
            ApplyCurve();
    }

    private void Update()
    {
        if (updateEveryFrame && !isApplyingCurve)
            ApplyCurve();
    }

    private void OnTextChanged(Object obj)
    {
        if (isApplyingCurve) return;
        if (obj == textComponent)
            ApplyCurve();
    }

    public void SetCurvature(float value)
    {
        curvature = value;
        ApplyCurve();
    }

    public void ApplyCurve()
    {
        if (isApplyingCurve) return;
        if (textComponent == null) return;

        isApplyingCurve = true;

        try
        {
            textComponent.ForceMeshUpdate();

            TMP_TextInfo textInfo = textComponent.textInfo;

            if (textInfo == null || textInfo.meshInfo == null)
                return;

            int characterCount = textInfo.characterCount;

            if (characterCount == 0) return;

            Bounds bounds = textComponent.textBounds;
            float textWidth = Mathf.Max(bounds.size.x, 0.0001f);

            bool curveUp = curvature >= 0f;
            float absCurvature = Mathf.Abs(curvature);

            if (absCurvature < 0.01f)
                return;

            float radius = (textWidth * 50f) / (Mathf.PI * absCurvature);
            float sign = curveUp ? 1f : -1f;

            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                // Güvenlik kontrolü: materialIndex ve vertex aralýðý mesh ile tutarlý mý?
                if (materialIndex < 0 || materialIndex >= textInfo.meshInfo.Length)
                    continue;

                Vector3[] sourceVertices = textInfo.meshInfo[materialIndex].vertices;

                if (sourceVertices == null || vertexIndex + 3 >= sourceVertices.Length)
                    continue;

                Vector3 charMidBaseline = new Vector3(
                    (sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) * 0.5f,
                    charInfo.baseLine,
                    0f
                );

                for (int v = 0; v < 4; v++)
                    worldVertices[v] = sourceVertices[vertexIndex + v] - charMidBaseline;

                float xOffsetFromCenter = charMidBaseline.x - bounds.center.x;
                float theta = xOffsetFromCenter / radius;

                float localX = radius * Mathf.Sin(theta);
                float localY = sign * radius * (Mathf.Cos(theta) - 1f);

                float rotationAngleRad = curveUp ? -theta : theta;
                Quaternion rotation = rotateCharacters
                    ? Quaternion.Euler(0f, 0f, rotationAngleRad * Mathf.Rad2Deg)
                    : Quaternion.identity;

                Vector3 anchor = new Vector3(bounds.center.x, charInfo.baseLine, 0f);
                Vector3 curvedOffset = new Vector3(localX, localY, 0f);

                for (int v = 0; v < 4; v++)
                {
                    Vector3 localOffset = rotateCharacters ? rotation * worldVertices[v] : worldVertices[v];
                    sourceVertices[vertexIndex + v] = anchor + curvedOffset + localOffset;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                if (textInfo.meshInfo[i].mesh == null) continue;

                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
        }
        finally
        {
            isApplyingCurve = false;
        }
    }
}