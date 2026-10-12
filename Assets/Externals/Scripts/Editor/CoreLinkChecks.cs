using System.Collections;
using System.Reflection;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class CoreLinkChecks
    {
        public static string Result { get; private set; } = "Not run";
        public static void Run()
        {
            CoreLinkView links = Object.FindAnyObjectByType<CoreLinkView>();
            Result = "Running";
            links.StartCoroutine(Check(links));
        }

        private static IEnumerator Check(CoreLinkView links)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            EnemyManager manager = Object.FindAnyObjectByType<EnemyManager>();
            CoreState core = (CoreState)typeof(EnemyManager).GetField("coreState", flags).GetValue(manager);
            CoreView view = links.GetComponent<CoreView>();
            PlayerController controls = Object.FindAnyObjectByType<PlayerController>();
            LineRenderer[] lines = (LineRenderer[])typeof(CoreLinkView).GetField("lines", flags).GetValue(links);
            int[] saved = core.Capture();
            float scale = Time.timeScale;
            core.TryDamage(core.GetCell(0), 0, "Core link test");
            view.Apply(core);
            if (Time.timeScale != 0f || !controls.PresentationInputLocked || controls.TryMove(Vector2Int.right) || controls.TryToggleShield())
            { Result = "FAILED: hit stop/input lock"; yield break; }

            int offEdges = 0;
            bool previous = lines[0].enabled;
            float deadline = Time.unscaledTime + 3f;
            while (Time.unscaledTime < deadline)
            {
                if (previous && !lines[0].enabled) offEdges++;
                previous = lines[0].enabled;
                if (!lines[1].enabled || !lines[2].enabled || !lines[3].enabled)
                { Result = "FAILED: unaffected link disabled"; yield break; }
                yield return null;
            }
            if (offEdges != 3 || lines[0].enabled || Time.timeScale != scale || controls.PresentationInputLocked)
            { Result = $"FAILED: three blinks/release ({offEdges})"; yield break; }

            core.Restore(saved);
            view.Apply(core);
            if (Vector3.Distance(lines[0].GetPosition(0), lines[0].GetPosition(lines[0].positionCount - 1)) > 0.001f)
            { Result = "FAILED: reconnect did not start from core"; yield break; }
            yield return new WaitForSecondsRealtime(0.25f);
            float partial = Vector3.Distance(lines[0].GetPosition(0), lines[0].GetPosition(lines[0].positionCount - 1));
            yield return new WaitForSecondsRealtime(0.35f);
            float full = Vector3.Distance(lines[0].GetPosition(0), lines[0].GetPosition(lines[0].positionCount - 1));
            if (!(partial > 0f && partial < full && lines[0].enabled))
            { Result = "FAILED: reconnect interpolation"; yield break; }

            controls.TryMove(Vector2Int.right);
            core.TryDamage(core.GetCell(0), controls.CurrentTurn, "Undo link test");
            view.Apply(core);
            if (!controls.TryUndo() || !lines[0].enabled || Time.timeScale != scale || controls.PresentationInputLocked)
            { Result = "FAILED: undo during hit stop"; yield break; }
            Result = "PASSED: four links, hit stop, three blinks, reconnection and undo cancellation";
            Debug.Log("[CORE LINK CHECKS] " + Result);
        }
    }
}
