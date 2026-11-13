using UnityEngine;

namespace FourCore
{
    // Plays a PIXELFX clip on a SpriteRenderer. One-shots destroy their GameObject when done.
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class PixelFxPlayer : MonoBehaviour
    {
        private static Material sharedMaterial;
        private SpriteRenderer spriteRenderer;
        private PixelFxClip clip;
        private float age;
        private bool destroyWhenDone;

        // Effects keep playing through the core-damage hit-stop (timeScale 0) and the game-over pause.
        public bool UseUnscaledTime { get; set; } = true;
        public bool IsDone => clip == null || (!clip.Loop && age >= clip.Duration);
        public string ClipKey => clip?.Key;

        public static Material SharedMaterial
        {
            get
            {
                if (sharedMaterial != null) return sharedMaterial;
                sharedMaterial = Resources.Load<Material>(PixelFxClip.ResourceFolder + "PixelFXMaterial");
                if (sharedMaterial == null)
                {
                    Shader shader = Shader.Find("4CORE/PixelFX Sprite");
                    if (shader != null) sharedMaterial = new Material(shader) { name = "PixelFX (Runtime)" };
                }
                return sharedMaterial;
            }
        }

        public static PixelFxPlayer Attach(SpriteRenderer target, string key, bool destroyWhenDone)
        {
            PixelFxPlayer player = target.GetComponent<PixelFxPlayer>();
            if (player == null) player = target.gameObject.AddComponent<PixelFxPlayer>();
            player.destroyWhenDone = destroyWhenDone;
            player.Play(key);
            return player;
        }

        public void Play(string key, bool restart = true)
        {
            spriteRenderer ??= GetComponent<SpriteRenderer>();
            if (SharedMaterial != null) spriteRenderer.sharedMaterial = SharedMaterial;
            PixelFxClip next = PixelFxClip.Load(key);
            if (next == clip && !restart) return;
            clip = next;
            age = 0f;
            Refresh();
        }

        private void Update()
        {
            if (clip == null) return;
            age += UseUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            if (IsDone)
            {
                spriteRenderer.sprite = null;
                if (destroyWhenDone) Destroy(gameObject);
                return;
            }
            Refresh();
        }

        private void Refresh()
        {
            if (clip != null) spriteRenderer.sprite = clip.Frames[clip.FrameAt(age)];
        }
    }
}
