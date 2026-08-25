using System.Collections;
using UnityEngine;

namespace TPS.Enemy.Presentation
{
    [DisallowMultipleComponent]
    public sealed class EnemyHitFlash : MonoBehaviour
    {
        [SerializeField, InspectorName("闪白颜色")]
        private Color flashColor = Color.red;

        [SerializeField, InspectorName("闪白时间"), Min(0.01f)]
        private float flashDuration = 0.2f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private Renderer[] renderers;
        private Color[] originalColors;
        private MaterialPropertyBlock propertyBlock;
        private Coroutine flashRoutine;

        private void Awake()
        {
            CacheRenderers();
        }

        private void OnDisable()
        {
            ResetFlash();
        }

        public void Play()
        {
            if (!isActiveAndEnabled || renderers == null || renderers.Length == 0)
            {
                return;
            }

            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashRoutine());
        }

        public void ResetFlash()
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
                flashRoutine = null;
            }

            RestoreColor();
        }

        private IEnumerator FlashRoutine()
        {
            SetColor(flashColor);
            yield return new WaitForSeconds(flashDuration);
            RestoreColor();
            flashRoutine = null;
        }

        private void CacheRenderers()
        {
            renderers = GetComponentsInChildren<Renderer>(true);
            originalColors = new Color[renderers.Length];
            propertyBlock = new MaterialPropertyBlock();

            for (int i = 0; i < renderers.Length; i++)
            {
                originalColors[i] = ResolveOriginalColor(renderers[i]);
            }
        }

        private static Color ResolveOriginalColor(Renderer targetRenderer)
        {
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                return Color.white;
            }

            Material material = targetRenderer.sharedMaterial;
            if (material.HasProperty(BaseColorId))
            {
                return material.GetColor(BaseColorId);
            }

            return material.HasProperty(ColorId) ? material.GetColor(ColorId) : Color.white;
        }

        private void SetColor(Color color)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void RestoreColor()
        {
            if (renderers == null || originalColors == null)
            {
                return;
            }

            propertyBlock ??= new MaterialPropertyBlock();
            int count = Mathf.Min(renderers.Length, originalColors.Length);
            for (int i = 0; i < count; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, originalColors[i]);
                propertyBlock.SetColor(ColorId, originalColors[i]);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }
}
