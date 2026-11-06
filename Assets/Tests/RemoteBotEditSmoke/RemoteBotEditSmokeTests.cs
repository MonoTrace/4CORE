using NUnit.Framework;
using UnityEditor;
using System.Linq;

public class RemoteBotEditSmokeTests
{
    [Test]
    public void BuildHasEnabledScene()
    {
        Assert.IsTrue(EditorBuildSettings.scenes.Any(scene => scene.enabled),
            "Build Profiles / Scene List에 게임 씬을 추가하세요.");
    }
}
