using System;
using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

public class RemoteBotSmokeTests
{
    [Serializable]
    private class RecordingInfo
    {
        public int fps, frameCount, width, height, firstUnityFrame, lastUnityFrame;
        public float firstGameTime, lastGameTime;
    }
    [Test]
    public void BuildContainsAScene()
    {
        Assert.Greater(SceneManager.sceneCountInBuildSettings, 0,
            "Add an enabled game scene to Build Profiles / Scene List.");
    }
    [UnityTest]
    public IEnumerator FirstSceneAdvancesAndCaptures()
    {
        Assert.Greater(SceneManager.sceneCountInBuildSettings, 0);
        yield return SceneManager.LoadSceneAsync(0);
        int before = Time.frameCount;
        for (int i = 0; i < 30; i++) yield return null;
        Assert.Greater(Time.frameCount, before + 20, "Game frames must advance.");
        Assert.Greater(Time.timeScale, 0, "Game must not be paused.");
        Assert.Greater(Camera.allCamerasCount, 0, "Loaded scene needs an active camera.");
        string output = Environment.GetEnvironmentVariable("UNITY_AI_CAPTURE_PATH");
        string frames = Environment.GetEnvironmentVariable("UNITY_AI_VIDEO_FRAMES");
        bool recording = !string.IsNullOrEmpty(frames);
        if (string.IsNullOrEmpty(output) && !recording) yield break;
        int fps = 24, frameCount = 1;
        if (recording)
        {
            Assert.IsTrue(int.TryParse(Environment.GetEnvironmentVariable("UNITY_AI_VIDEO_FPS"), out fps));
            Assert.IsTrue(int.TryParse(Environment.GetEnvironmentVariable("UNITY_AI_VIDEO_COUNT"), out frameCount));
            Assert.That(fps, Is.InRange(10, 30));
            Assert.That(frameCount, Is.InRange(fps * 2, fps * 20));
            Directory.CreateDirectory(frames);
        }
        var info = new RecordingInfo { fps = fps, frameCount = frameCount, width = 960, height = 540 };
        var target = new RenderTexture(info.width, info.height, 24);
        var image = new Texture2D(info.width, info.height, TextureFormat.RGB24, false);
        float previousCaptureDelta = Time.captureDeltaTime;
        try
        {
            target.Create();
            // Advance the live scene once per recorded frame, even when PNG encoding is slow.
            if (recording) Time.captureDeltaTime = 1f / fps;
            for (int i = 0; i < frameCount; i++)
            {
                yield return null;
                if (i == 0) { info.firstUnityFrame = Time.frameCount; info.firstGameTime = Time.time; }
                var camera = Camera.main != null ? Camera.main : Camera.allCameras[0];
                RenderCamera(camera, target);
                var previousActive = RenderTexture.active;
                try
                {
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                    image.Apply();
                }
                finally { RenderTexture.active = previousActive; }
                bool hasVisiblePixels = false;
                foreach (var pixel in image.GetPixels32())
                {
                    if (pixel.r > 8 || pixel.g > 8 || pixel.b > 8)
                    { hasVisiblePixels = true; break; }
                }
                Assert.IsTrue(hasVisiblePixels, "Capture is black; inspect rendering before approving.");
                var png = image.EncodeToPNG();
                if (recording) File.WriteAllBytes(Path.Combine(frames, "frame_" + i.ToString("D5") + ".png"), png);
                if (!string.IsNullOrEmpty(output) && i == frameCount / 2)
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(output));
                    File.WriteAllBytes(output, png);
                }
            }
            info.lastUnityFrame = Time.frameCount; info.lastGameTime = Time.time;
            if (recording)
            {
                Assert.GreaterOrEqual(info.lastUnityFrame - info.firstUnityFrame, frameCount - 1);
                Assert.Greater(info.lastGameTime, info.firstGameTime);
                File.WriteAllText(Path.Combine(frames, "recording.json"), JsonUtility.ToJson(info, true));
            }
        }
        finally
        {
            Time.captureDeltaTime = previousCaptureDelta;
            target.Release(); UnityEngine.Object.Destroy(target); UnityEngine.Object.Destroy(image);
        }
    }
    // Camera output only: no desktop, audio or screen-space Overlay UI is recorded.
    private static void RenderCamera(Camera camera, RenderTexture target)
    {
        if (GraphicsSettings.currentRenderPipeline != null)
        {
            var request = new RenderPipeline.StandardRequest { destination = target };
            Assert.IsTrue(RenderPipeline.SupportsRenderRequest(camera, request),
                "The active render pipeline must support camera render requests.");
            RenderPipeline.SubmitRenderRequest(camera, request);
        }
        else
        {
            var previousTarget = camera.targetTexture;
            try { camera.targetTexture = target; camera.Render(); }
            finally { camera.targetTexture = previousTarget; }
        }
    }
}
