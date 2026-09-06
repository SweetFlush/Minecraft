using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VR_Minecraft
{
    public class VR_Crosshair : MonoBehaviour
    {
        [Header("Appearance")]
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.7f);
        [SerializeField] private Color targetOnBlockColor = new Color(1f, 0.9f, 0.2f, 0.95f);
        [SerializeField] private float baseScale = 0.05f;
        [SerializeField] private bool scaleWithDistance = true;
        [SerializeField] private float distanceScaleFactor = 0.02f;

        private MaterialPropertyBlock propBlock;
        private static readonly int ColorPropId = Shader.PropertyToID("_Color");
        private static readonly int BaseColorPropId = Shader.PropertyToID("_BaseColor");

        private void Awake()
        {
            if (meshRenderer == null)
            {
                meshRenderer = GetComponentInChildren<MeshRenderer>();
            }
            propBlock = new MaterialPropertyBlock();
        }

        public void SetTarget(Vector3 worldPosition, Vector3 surfaceNormal, bool onBlock, float distance)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            // 표면에서 약간 띄워 z-fighting 방지
            transform.position = worldPosition + surfaceNormal * 0.015f;

            if (surfaceNormal != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(surfaceNormal);
            }

            if (scaleWithDistance)
            {
                float scale = baseScale + distance * distanceScaleFactor;
                transform.localScale = Vector3.one * scale;
            }

            SetColor(onBlock ? targetOnBlockColor : normalColor);
        }

        public void SetFreePosition(Vector3 worldPosition, Vector3 forwardDirection, float distance)
        {
            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            transform.position = worldPosition;
            if (forwardDirection != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(forwardDirection);
            }

            if (scaleWithDistance)
            {
                float scale = baseScale + distance * distanceScaleFactor;
                transform.localScale = Vector3.one * scale;
            }

            SetColor(normalColor);
        }

        public void SetVisible(bool visible)
        {
            if (gameObject.activeSelf != visible)
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetColor(Color color)
        {
            if (meshRenderer == null) return;

            meshRenderer.GetPropertyBlock(propBlock);
            propBlock.SetColor(ColorPropId, color);
            propBlock.SetColor(BaseColorPropId, color);
            meshRenderer.SetPropertyBlock(propBlock);
        }
    }
}
