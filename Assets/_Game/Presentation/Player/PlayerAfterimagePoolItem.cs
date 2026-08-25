using System.Collections.Generic;
using TPS.Player.Infrastructure;
using UnityEngine;
using UnityEngine.Rendering;

namespace TPS.Player.Presentation
{
    [DisallowMultipleComponent]
    public sealed class PlayerAfterimagePoolItem : MonoBehaviour, IPoolableObject
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");
        private static readonly int SurfaceId = Shader.PropertyToID("_Surface");
        private static readonly int SrcBlendId = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendId = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteId = Shader.PropertyToID("_ZWrite");

        private sealed class RenderSlot
        {
            public Transform Transform;
            public MeshFilter Filter;
            public MeshRenderer Renderer;
            public Mesh Mesh;
            public Material[] Materials;
            public bool ScaleModeResolved;
            public bool BakeWithScale;
        }

        private readonly List<RenderSlot> slots = new();
        private static bool scaleDiagnosticsLogged;
        private static bool invalidMeshDiagnosticsLogged;
        private PoolItem poolItem;
        private float elapsed;
        private float lifetime;
        private float startAlpha;
        private Color color;

        public bool Capture(
            SkinnedMeshRenderer[] sources,
            PlayerConfig config,
            Transform playerRoot,
            Transform characterRoot,
            float lifetimeOverride = -1f)
        {
            elapsed = 0f;
            lifetime = lifetimeOverride > 0f
                ? lifetimeOverride
                : config.AfterimageLifetime;
            startAlpha = config.AfterimageStartAlpha;
            color = config.AfterimageColor;
            EnsureSlotCount(sources.Length);
            bool capturedAnyRenderer = false;

            for (int i = 0; i < slots.Count; i++)
            {
                bool active = i < sources.Length
                    && sources[i] != null
                    && sources[i].enabled
                    && sources[i].gameObject.activeInHierarchy
                    && sources[i].sharedMesh != null;
                RenderSlot slot = slots[i];
                slot.Renderer.enabled = false;
                if (!active)
                {
                    continue;
                }

                SkinnedMeshRenderer source = sources[i];
                BakeCurrentPose(source, slot);
                slot.Filter.sharedMesh = null;
                slot.Filter.sharedMesh = slot.Mesh;
                if (slot.Mesh.vertexCount == 0
                    || slot.Mesh.bounds.size.sqrMagnitude <= 0.000001f)
                {
                    LogInvalidMesh(config, source, slot.Mesh);
                    continue;
                }

                slot.Transform.SetPositionAndRotation(
                    source.transform.position,
                    source.transform.rotation);
                slot.Transform.localScale = Vector3.one;
                slot.Transform.gameObject.layer = source.gameObject.layer;
                EnsureMaterials(slot, source.sharedMaterials);
                ApplyAlpha(slot, startAlpha);
                slot.Renderer.forceRenderingOff = false;
                slot.Renderer.enabled = true;
                capturedAnyRenderer = true;
                LogScaleDiagnostics(config, playerRoot, characterRoot, source, slot);
            }

            return capturedAnyRenderer;
        }

        public void OnTakenFromPool()
        {
            elapsed = 0f;
            poolItem ??= GetComponent<PoolItem>();
        }

        public void OnReturnedToPool()
        {
            elapsed = 0f;
            lifetime = 0f;
            for (int i = 0; i < slots.Count; i++)
            {
                slots[i].Renderer.enabled = false;
            }
        }

        private void Update()
        {
            if (lifetime <= 0f)
            {
                return;
            }

            elapsed += Time.unscaledDeltaTime;
            float alpha = startAlpha * (1f - Mathf.Clamp01(elapsed / lifetime));
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].Renderer.enabled)
                {
                    ApplyAlpha(slots[i], alpha);
                }
            }

            if (elapsed >= lifetime)
            {
                lifetime = 0f;
                poolItem ??= GetComponent<PoolItem>();
                poolItem?.ReturnToPool();
            }
        }

        private void OnDestroy()
        {
            for (int i = 0; i < slots.Count; i++)
            {
                RenderSlot slot = slots[i];
                if (slot.Mesh != null)
                {
                    Destroy(slot.Mesh);
                }

                if (slot.Materials == null)
                {
                    continue;
                }

                for (int materialIndex = 0; materialIndex < slot.Materials.Length; materialIndex++)
                {
                    if (slot.Materials[materialIndex] != null)
                    {
                        Destroy(slot.Materials[materialIndex]);
                    }
                }
            }
        }

        private void EnsureSlotCount(int count)
        {
            while (slots.Count < count)
            {
                GameObject slotObject = new GameObject($"AfterimageMesh_{slots.Count}");
                slotObject.transform.SetParent(transform, false);
                MeshFilter filter = slotObject.AddComponent<MeshFilter>();
                MeshRenderer renderer = slotObject.AddComponent<MeshRenderer>();
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.lightProbeUsage = LightProbeUsage.Off;
                renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;

                Mesh mesh = new Mesh
                {
                    name = $"PlayerAfterimageMesh_{slots.Count}",
                    hideFlags = HideFlags.DontSave
                };
                filter.sharedMesh = mesh;
                slots.Add(new RenderSlot
                {
                    Transform = slotObject.transform,
                    Filter = filter,
                    Renderer = renderer,
                    Mesh = mesh
                });
            }
        }

        private static void BakeCurrentPose(
            SkinnedMeshRenderer source,
            RenderSlot slot)
        {
            if (slot.ScaleModeResolved)
            {
                slot.Mesh.Clear();
                source.BakeMesh(slot.Mesh, slot.BakeWithScale);
                slot.Mesh.RecalculateBounds();
                return;
            }

            slot.Mesh.Clear();
            source.BakeMesh(slot.Mesh, false);
            slot.Mesh.RecalculateBounds();
            float withoutScaleError = CalculateBoundsError(source, slot.Mesh);
            if (withoutScaleError <= 0.05f)
            {
                slot.ScaleModeResolved = true;
                slot.BakeWithScale = false;
                return;
            }

            slot.Mesh.Clear();
            source.BakeMesh(slot.Mesh, true);
            slot.Mesh.RecalculateBounds();
            float withScaleError = CalculateBoundsError(source, slot.Mesh);
            slot.BakeWithScale = withScaleError < withoutScaleError;
            slot.ScaleModeResolved = true;
            if (slot.BakeWithScale)
            {
                return;
            }

            slot.Mesh.Clear();
            source.BakeMesh(slot.Mesh, false);
            slot.Mesh.RecalculateBounds();
        }

        private static float CalculateBoundsError(
            SkinnedMeshRenderer source,
            Mesh bakedMesh)
        {
            Vector3 expectedSize = source.bounds.size;
            Vector3 bakedWorldSize = CalculateWorldAabbSize(
                bakedMesh.bounds,
                source.transform.rotation);
            return (bakedWorldSize - expectedSize).magnitude
                / Mathf.Max(0.0001f, expectedSize.magnitude);
        }

        private static Vector3 CalculateWorldAabbSize(
            Bounds localBounds,
            Quaternion rotation)
        {
            Vector3 extents = localBounds.extents;
            Vector3 axisX = rotation * new Vector3(extents.x, 0f, 0f);
            Vector3 axisY = rotation * new Vector3(0f, extents.y, 0f);
            Vector3 axisZ = rotation * new Vector3(0f, 0f, extents.z);
            return new Vector3(
                Mathf.Abs(axisX.x) + Mathf.Abs(axisY.x) + Mathf.Abs(axisZ.x),
                Mathf.Abs(axisX.y) + Mathf.Abs(axisY.y) + Mathf.Abs(axisZ.y),
                Mathf.Abs(axisX.z) + Mathf.Abs(axisY.z) + Mathf.Abs(axisZ.z)) * 2f;
        }
        private static void EnsureMaterials(RenderSlot slot, Material[] sourceMaterials)
        {
            if (slot.Materials != null && slot.Materials.Length == sourceMaterials.Length)
            {
                slot.Renderer.sharedMaterials = slot.Materials;
                return;
            }

            if (slot.Materials != null)
            {
                for (int i = 0; i < slot.Materials.Length; i++)
                {
                    if (slot.Materials[i] != null)
                    {
                        Destroy(slot.Materials[i]);
                    }
                }
            }

            slot.Materials = new Material[sourceMaterials.Length];
            for (int i = 0; i < sourceMaterials.Length; i++)
            {
                Material source = sourceMaterials[i];
                if (source == null)
                {
                    continue;
                }

                Material material = new Material(source)
                {
                    name = $"{source.name}_Afterimage",
                    hideFlags = HideFlags.DontSave,
                    renderQueue = (int)RenderQueue.Transparent
                };
                material.SetOverrideTag("RenderType", "Transparent");
                if (material.HasProperty(SurfaceId))
                {
                    material.SetFloat(SurfaceId, 1f);
                }

                if (material.HasProperty(SrcBlendId))
                {
                    material.SetInt(SrcBlendId, (int)BlendMode.SrcAlpha);
                }

                if (material.HasProperty(DstBlendId))
                {
                    material.SetInt(DstBlendId, (int)BlendMode.OneMinusSrcAlpha);
                }

                if (material.HasProperty(ZWriteId))
                {
                    material.SetInt(ZWriteId, 0);
                }

                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                material.DisableKeyword("_ALPHATEST_ON");
                slot.Materials[i] = material;
            }

            slot.Renderer.sharedMaterials = slot.Materials;
        }

        private static void LogScaleDiagnostics(
            PlayerConfig config,
            Transform playerRoot,
            Transform characterRoot,
            SkinnedMeshRenderer source,
            RenderSlot slot)
        {
            if (!config.AfterimageScaleDiagnostics || scaleDiagnosticsLogged)
            {
                return;
            }

            scaleDiagnosticsLogged = true;
            Transform ghostParent = slot.Transform.parent;
            Debug.Log(
                $"[PlayerAfterimageScale] " +
                $"PlayerRoot={DescribeTransform(playerRoot)} | " +
                $"CharacterRoot={DescribeTransform(characterRoot)} | " +
                $"Model={DescribeTransform(source.transform)} | " +
                $"Ghost={DescribeTransform(slot.Transform)} | " +
                $"GhostParent={DescribeTransform(ghostParent)} | " +
                $"SourcePosition={source.transform.position} | " +
                $"GhostPosition={slot.Transform.position} | " +
                $"MeshVertices={slot.Mesh.vertexCount} | " +
                $"BakeWithScale={slot.BakeWithScale} | " +
                $"BakedMeshBounds={slot.Mesh.bounds.size} | " +
                $"SourceBounds={source.bounds.size} | " +
                $"GhostBounds={slot.Renderer.bounds.size}",
                slot.Renderer);
        }

        private static void LogInvalidMesh(
            PlayerConfig config,
            SkinnedMeshRenderer source,
            Mesh mesh)
        {
            if (!config.AfterimageScaleDiagnostics || invalidMeshDiagnosticsLogged)
            {
                return;
            }

            invalidMeshDiagnosticsLogged = true;
            Debug.LogWarning(
                $"[PlayerAfterimageSpawn] Invalid baked mesh. " +
                $"Source={source.name}, vertices={mesh.vertexCount}, " +
                $"bounds={mesh.bounds.size}",
                source);
        }

        private static string DescribeTransform(Transform target)
        {
            return target == null
                ? "null"
                : $"{target.name}(local={target.localScale}, lossy={target.lossyScale})";
        }

        private void ApplyAlpha(RenderSlot slot, float alpha)
        {
            Color tint = color;
            tint.a = alpha;
            for (int i = 0; i < slot.Materials.Length; i++)
            {
                Material material = slot.Materials[i];
                if (material == null)
                {
                    continue;
                }

                if (material.HasProperty(BaseColorId))
                {
                    material.SetColor(BaseColorId, tint);
                }

                if (material.HasProperty(ColorId))
                {
                    material.SetColor(ColorId, tint);
                }
            }
        }
    }
}
