using System;
using Febucci.TextAnimatorForUnity;
using Febucci.TextAnimatorForUnity.Effects.Core;
using Febucci.TextAnimatorForUnity.TextMeshPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace FourCore.EditorTools
{
    public static class ScoreFeedbackSetup
    {
        private const string Folder = "Assets/Externals/Datas/ScoreFeedback";
        private const string Plugin = "Assets/Plugins/Febucci/Text Animator for Unity";

        [MenuItem("4CORE/Set Up Score Text Animator")]
        public static void Run()
        {
            if (EditorApplication.isPlaying)
                throw new InvalidOperationException("Stop Play mode before configuring the score HUD.");
            ScoreHud hud = UnityEngine.Object.FindAnyObjectByType<ScoreHud>();
            if (hud == null) throw new InvalidOperationException("The scene needs a ScoreHud.");
            Configure(hud);
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkSceneDirty(hud.gameObject.scene);
            if (!EditorSceneManager.SaveScene(hud.gameObject.scene))
                throw new InvalidOperationException("Could not save the scene.");
        }

        public static void Configure(ScoreHud hud)
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Externals/Datas", "ScoreFeedback");

            // Dedicated assets keep score tuning separate from the plugin's global effects.
            string playbackPath = Folder + "/ScorePopPlayback.asset";
            ScriptableObject playback = AssetDatabase.LoadAssetAtPath<ScriptableObject>(playbackPath);
            if (playback == null)
            {
                playback = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    Plugin + "/Playbacks/Once Playback.asset"));
                playback.name = "ScorePopPlayback";
                SerializedObject serialized = new(playback);
                serialized.FindProperty("tagID").stringValue = "scorepopplayback";
                serialized.FindProperty("playback.delayBeforeStart").floatValue = 0f;
                serialized.FindProperty("playback.cycles").intValue = 1;
                serialized.FindProperty("playback.delayBetweenCycles").floatValue = 0f;
                serialized.FindProperty("playback.fadeInDuration").floatValue = 0.055f;
                serialized.FindProperty("playback.stillDuration").floatValue = 0.025f;
                serialized.FindProperty("playback.fadeOutDuration").floatValue = 0.24f;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(playback, playbackPath);
            }

            string effectPath = Folder + "/ScorePopEffect.asset";
            EffectScriptableBase effect = AssetDatabase.LoadAssetAtPath<EffectScriptableBase>(effectPath);
            if (effect == null)
            {
                effect = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<EffectScriptableBase>(
                    Plugin + "/Effects/Size Effect.asset"));
                effect.name = "ScorePopEffect";
                effect.TagID = "scorepop";
                SerializedObject serialized = new(effect);
                serialized.FindProperty("persistent.stateParams.scale").vector3Value = new Vector3(1.28f, 1.28f, 1f);
                serialized.FindProperty("persistent.phaseParams.charOffset").floatValue = 0f;
                serialized.FindProperty("persistent.phaseParams.wordOffset").floatValue = 0f;
                serialized.FindProperty("persistent.phaseParams.speed").floatValue = 1f;
                serialized.FindProperty("persistent.playback").objectReferenceValue = playback;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                AssetDatabase.CreateAsset(effect, effectPath);
            }

            string databasePath = Folder + "/ScoreEffectsDatabase.asset";
            AnimationsDatabase database = AssetDatabase.LoadAssetAtPath<AnimationsDatabase>(databasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<AnimationsDatabase>();
                database.name = "ScoreEffectsDatabase";
                database.Add(effect);
                AssetDatabase.CreateAsset(database, databasePath);
            }

            SerializedObject score = new(hud);
            TMPro.TMP_Text label = score.FindProperty("scoreText").objectReferenceValue as TMPro.TMP_Text;
            if (label == null) throw new InvalidOperationException("Assign ScoreHud's text before configuring feedback.");
            TextAnimator_TMP animator = label.GetComponent<TextAnimator_TMP>();
            if (animator == null)
            {
                animator = Undo.AddComponent<TextAnimator_TMP>(label.gameObject);
                animator.DatabaseEffects = database;
                animator.localSettings.timeScale = Febucci.TextAnimatorCore.Time.TimeScale.Unscaled;
                animator.localSettings.isResettingTimeOnNewText = true;
                EditorUtility.SetDirty(animator);
            }
            score.FindProperty("textAnimator").objectReferenceValue = animator;
            score.ApplyModifiedProperties();
        }
    }
}
