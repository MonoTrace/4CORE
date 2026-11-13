using UnityEngine;
using UnityEngine.UI;

namespace FourCore
{
    // Plays a PIXELFX clip on a uGUI Image, for HUD effects. One-shots destroy their GameObject when done.
    [RequireComponent(typeof(Image))]
    public sealed class PixelFxImage : MonoBehaviour
    {
        private Image image;
        private PixelFxClip clip;
        private float age;

        public static PixelFxImage Play(string key, RectTransform parent, Vector2 anchoredPosition, float pixelScale)
        {
            PixelFxClip clip = PixelFxClip.Load(key);
            if (clip == null || parent == null) return null;
            GameObject go = new($"PixelFX {key}", typeof(RectTransform), typeof(Image), typeof(PixelFxImage));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = clip.Pivot;
            rect.sizeDelta = (Vector2)clip.FrameSize * pixelScale;
            rect.anchoredPosition = anchoredPosition;
            PixelFxImage player = go.GetComponent<PixelFxImage>();
            player.image = go.GetComponent<Image>();
            player.image.raycastTarget = false;
            player.image.preserveAspect = false;
            player.clip = clip;
            player.image.sprite = clip.Frames[0];
            return player;
        }

        private void Update()
        {
            if (clip == null) return;
            age += Time.unscaledDeltaTime;
            if (!clip.Loop && age >= clip.Duration)
            {
                Destroy(gameObject);
                return;
            }
            image.sprite = clip.Frames[clip.FrameAt(age)];
        }
    }
}
