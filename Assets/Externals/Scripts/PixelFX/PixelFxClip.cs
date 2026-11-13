using System;
using System.Collections.Generic;
using UnityEngine;

namespace FourCore
{
    // A PIXELFX sprite sheet (PNG + Aseprite-style JSON) baked by the pixelfx tools.
    // Sheets live in Assets/Externals/PixelFX/Resources/PixelFX/<key>.png and <key>.json.
    public sealed class PixelFxClip
    {
        [Serializable] public sealed class SheetJson { public MetaJson meta; }
        [Serializable] public sealed class MetaJson { public PixelJson pixelfx; }
        [Serializable] public sealed class PixelJson
        {
            public int fps = 30;
            public bool loop;
            public int frames;
            public int[] frameSize;
            public int[] pivot;
            public int columns = 1;
        }

        public const string ResourceFolder = "PixelFX/";
        public const float DefaultPixelsPerUnit = 48f;
        private static readonly Dictionary<string, PixelFxClip> Cache = new();

        public string Key { get; }
        public Texture2D Texture { get; }
        public Sprite[] Frames { get; }
        public int Fps { get; }
        public bool Loop { get; }
        public Vector2Int FrameSize { get; }
        public Vector2 Pivot { get; }
        public float Duration => Frames.Length / (float)Fps;

        private readonly Vector4[] uvRects;

        private PixelFxClip(string key, Texture2D texture, PixelJson data)
        {
            Key = key;
            Texture = texture;
            Fps = Mathf.Max(1, data.fps);
            Loop = data.loop;
            int w = data.frameSize[0], h = data.frameSize[1];
            FrameSize = new Vector2Int(w, h);
            Pivot = new Vector2(data.pivot[0] / (float)w, 1f - data.pivot[1] / (float)h);
            Frames = new Sprite[data.frames];
            uvRects = new Vector4[data.frames];
            int columns = Mathf.Max(1, data.columns);
            for (int i = 0; i < data.frames; i++)
            {
                int x = i % columns * w;
                int yTop = i / columns * h;
                Rect rect = new(x, texture.height - yTop - h, w, h);
                Frames[i] = Sprite.Create(texture, rect, Pivot, DefaultPixelsPerUnit, 0, SpriteMeshType.FullRect);
                Frames[i].name = $"{key} {i}";
                uvRects[i] = new Vector4(w / (float)texture.width, h / (float)texture.height,
                    rect.x / texture.width, rect.y / texture.height);
            }
        }

        public static bool Exists(string key) => Load(key) != null;

        public static PixelFxClip Load(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;
            if (Cache.TryGetValue(key, out PixelFxClip cached)) return cached;
            Texture2D texture = Resources.Load<Texture2D>(ResourceFolder + key);
            TextAsset json = Resources.Load<TextAsset>(ResourceFolder + key);
            PixelFxClip clip = null;
            if (texture != null && json != null)
            {
                PixelJson data = JsonUtility.FromJson<SheetJson>(json.text)?.meta?.pixelfx;
                if (data != null && data.frames > 0 && data.frameSize is { Length: 2 } && data.pivot is { Length: 2 })
                {
                    texture.filterMode = FilterMode.Point;
                    texture.wrapMode = TextureWrapMode.Clamp;
                    clip = new PixelFxClip(key, texture, data);
                }
            }
            if (clip == null) Debug.LogWarning($"PIXELFX clip '{key}' is missing from Resources/{ResourceFolder}.");
            Cache[key] = clip;
            return clip;
        }

        public int FrameAt(float time)
        {
            int frame = Mathf.FloorToInt(time * Fps);
            return Loop ? (frame % Frames.Length + Frames.Length) % Frames.Length : Mathf.Clamp(frame, 0, Frames.Length - 1);
        }

        // Scale (xy) and offset (zw) of a frame in the sheet, for _MainTex_ST on non-sprite renderers.
        public Vector4 UvRect(int frame) => uvRects[frame];
    }
}
