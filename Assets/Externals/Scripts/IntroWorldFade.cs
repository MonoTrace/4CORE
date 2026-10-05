using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FourCore
{
    // Use temporary transparent materials so shared prefab assets remain untouched.
    public sealed class IntroWorldFade : IDisposable
    {
        private sealed class MeshEntry
        {
            public MeshRenderer Renderer;
            public Material[] Originals, Temporary;
            public Color[] Colors;
        }

        private readonly List<MeshEntry> meshes = new();
        private readonly List<(SpriteRenderer renderer, Color color)> sprites = new();

        public IntroWorldFade(params GameObject[] roots)
        {
            foreach (GameObject root in roots)
            {
                foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    if (!renderer.gameObject.activeSelf) continue;
                    Material[] originals = renderer.sharedMaterials;
                    MeshEntry entry = new() { Renderer = renderer, Originals = originals,
                        Temporary = new Material[originals.Length], Colors = new Color[originals.Length] };
                    for (int i = 0; i < originals.Length; i++)
                    {
                        if (originals[i] == null) continue;
                        Material material = new(originals[i]) { name = originals[i].name + " (Intro Fade)" };
                        entry.Temporary[i] = material;
                        entry.Colors[i] = material.color;
                        material.SetOverrideTag("RenderType", "Transparent");
                        material.renderQueue = (int)RenderQueue.Transparent;
                        if (material.HasProperty("_Surface")) material.SetFloat("_Surface", 1f);
                        if (material.HasProperty("_SrcBlend")) material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
                        if (material.HasProperty("_DstBlend")) material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
                        if (material.HasProperty("_ZWrite")) material.SetFloat("_ZWrite", 0f);
                        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                    }
                    renderer.sharedMaterials = entry.Temporary;
                    meshes.Add(entry);
                }
                foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
                    sprites.Add((renderer, renderer.color));
            }
            SetOpacity(0f);
        }

        public void SetOpacity(float opacity)
        {
            opacity = Mathf.Clamp01(opacity);
            foreach (MeshEntry entry in meshes)
                for (int i = 0; i < entry.Temporary.Length; i++)
                {
                    if (entry.Temporary[i] == null) continue;
                    Color color = entry.Colors[i];
                    color.a *= opacity;
                    entry.Temporary[i].color = color;
                }
            foreach (var entry in sprites)
            {
                if (entry.renderer == null) continue;
                Color color = entry.color;
                color.a *= opacity;
                entry.renderer.color = color;
            }
        }

        public void Dispose()
        {
            foreach (MeshEntry entry in meshes)
            {
                if (entry.Renderer != null) entry.Renderer.sharedMaterials = entry.Originals;
                foreach (Material material in entry.Temporary)
                    if (material != null) UnityEngine.Object.Destroy(material);
            }
            foreach (var entry in sprites)
                if (entry.renderer != null) entry.renderer.color = entry.color;
            meshes.Clear();
            sprites.Clear();
        }
    }
}
