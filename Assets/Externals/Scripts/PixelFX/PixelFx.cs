using UnityEngine;

namespace FourCore
{
    // Entry point for one-shot PIXELFX effects in the world.
    public static class PixelFx
    {
        public const int DefaultSortingOrder = 20;

        public static class Keys
        {
            public const string CoreBreak = "core-break";
            public const string CoreRecover = "core-recover";
            public const string CoreLink = "core-link";
            public const string ShieldBreak = "shield-break";
            public const string MissileReflect = "missile-reflect";
            public const string PlayerHit = "player-hit";
            public const string Missile = "missile";
            public const string MissileReflected = "missile-reflected";
            public const string ScoreGain = "score-gain";
            public static string EnemyDefeat(EnemyType type) => $"enemy-defeat-{type.ToString().ToLowerInvariant()}";
            public static string Score(int baseScore, int cores) => $"score-{baseScore}x{cores}";
        }

        // Spawns a one-shot at a local position under parent (or world position when parent is null).
        public static PixelFxPlayer Play(string key, Transform parent, Vector3 localPosition,
            float angle = 0f, float scale = 1f, int sortingOrder = DefaultSortingOrder)
        {
            if (!PixelFxClip.Exists(key)) return null;
            GameObject go = new($"PixelFX {key}");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = Vector3.one * scale;
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sortingOrder = sortingOrder;
            return PixelFxPlayer.Attach(renderer, key, destroyWhenDone: true);
        }

        public static float Angle(Vector2Int direction) => Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        public static void ClearChildren(Transform root)
        {
            foreach (PixelFxPlayer player in root.GetComponentsInChildren<PixelFxPlayer>(true))
            {
                // Only spawned one-shots own their GameObject; looping attachments (missiles) stay.
                if (!player.name.StartsWith("PixelFX ")) continue;
                player.gameObject.SetActive(false);
                Object.Destroy(player.gameObject);
            }
        }
    }
}
