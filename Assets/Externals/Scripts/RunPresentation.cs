using TMPro;
using UnityEngine;

namespace FourCore
{
    public sealed class RunPresentation : MonoBehaviour
    {
        [SerializeField] private TMP_Text noticeText;
        [SerializeField] private TMP_Text freezeCount;
        [SerializeField] private LineRenderer personalShield;
        [SerializeField] private UnityEngine.UI.Image freezeTint;
        [SerializeField] private UnityEngine.UI.Image progress;
        private EnemyManager encounter;
        private PlayerController controls;
        private Transform player;
        private float noticeRemaining, hitRemaining;
        private MeshRenderer playerRenderer;
        private MaterialPropertyBlock properties;
        private AudioSource audioSource;
        private AudioClip[] notes;
        private AudioClip undoClip;
        private int noteIndex;
        private bool wasPlaying;
        private Material freezeMaterial,originalFreezeMaterial;
        private bool wasFrozen;
        private float freezeAge,freezeAlpha;
        public void Initialize(EnemyManager encounter, PlayerController controls, PlayerView player)
        {
            this.encounter = encounter; this.controls = controls; this.player = player.transform;
            playerRenderer = player.GetComponentInChildren<MeshRenderer>();
            properties = new MaterialPropertyBlock();
            originalFreezeMaterial=freezeTint.material;freezeMaterial=new Material(originalFreezeMaterial);freezeTint.material=freezeMaterial;
            encounter.Run.Notice += ShowNotice;
            encounter.PlayerDamaged += Hit;
            controls.ActionNotice += ShowNotice;
            controls.ActionTaken += PlayNote;
            controls.ActionUndone+=UndoFeedback;
            audioSource = gameObject.AddComponent<AudioSource>(); audioSource.playOnAwake = false; audioSource.volume = 0.08f;
            notes = new AudioClip[16];
            int[] tones = { 0, 7, 12, 3, 7, 15, 12, 7, 5, 12, 17, 7, 3, 10, 15, 7 };
            for (int i = 0; i < notes.Length; i++)
            {
                const int rate = 16000;
                float[] samples = new float[2400];
                float frequency = 220f * Mathf.Pow(2f, tones[i] / 12f);
                for (int j = 0; j < samples.Length; j++)
                {
                    float t = j / (float)rate;
                    float envelope = Mathf.Min(1f,t / 0.008f) * Mathf.Exp(-t*24f);
                    samples[j] = (Mathf.Sin(t*frequency*Mathf.PI*2f) + Mathf.Sin(t*frequency*2f*Mathf.PI*2f)*0.12f) * envelope * 0.6f;
                }
                notes[i] = AudioClip.Create("Action note",samples.Length,1,rate,false); notes[i].SetData(samples,0);
                if(i==0)
                {
                    System.Array.Reverse(samples);
                    for(int j=samples.Length-120;j<samples.Length;j++)samples[j]*=(samples.Length-j)/120f;
                    undoClip=AudioClip.Create("Undo reverse",samples.Length,1,rate,false);undoClip.SetData(samples,0);
                }
            }
            ShowNotice(encounter.Run.StageLabel);
        }
        private void PlayNote()
        {
            audioSource.pitch = encounter.Run.FreezeMoves > 0 ? 0.65f : 1f;
            audioSource.PlayOneShot(notes[noteIndex++ % notes.Length]);
        }
        private void UndoFeedback()
        {
            hitRemaining=0f;playerRenderer.SetPropertyBlock(null);noticeRemaining=0f;
            wasFrozen=encounter.Run.FreezeMoves>0;freezeAge=wasFrozen?1f:0f;freezeAlpha=wasFrozen ? .12f : 0f;
            audioSource.Stop();audioSource.pitch=1f;audioSource.PlayOneShot(undoClip);ShowNotice("UNDO");
        }
        public void ShowNotice(string text)
        {
            noticeText.text = text; noticeRemaining = 1.2f;
        }
        private void Hit() { hitRemaining = 0.2f; ShowNotice("HIT"); }
        private void Update()
        {
            if (encounter == null) return;
            if (controls.GameplayInputEnabled && !wasPlaying)
                ShowNotice(encounter.Run.StageLabel);
            wasPlaying = controls.GameplayInputEnabled;
            noticeRemaining = Mathf.Max(0f,noticeRemaining-Time.unscaledDeltaTime);
            noticeText.gameObject.SetActive(controls.GameplayInputEnabled && noticeRemaining > 0f);
            if (noticeRemaining > 0f) noticeText.alpha = Mathf.Min(1f,noticeRemaining*4f);
            bool frozen = encounter.Run.FreezeMoves > 0;
            if(frozen&&!wasFrozen)
            {
                freezeAge=0f;Vector3 center=Camera.main.WorldToViewportPoint(player.position);
                freezeMaterial.SetVector("_Center",new Vector4(center.x,center.y,0f,0f));
            }
            wasFrozen=frozen;freezeAge+=Time.unscaledDeltaTime;
            freezeAlpha=Mathf.MoveTowards(freezeAlpha,frozen ? .12f : 0f,Time.unscaledDeltaTime*.8f);
            freezeMaterial.SetFloat("_Radius",Mathf.SmoothStep(0f,2.5f,freezeAge/.35f));
            freezeMaterial.SetFloat("_Aspect",Camera.main.aspect);
            freezeCount.gameObject.SetActive(frozen && controls.GameplayInputEnabled);
            freezeCount.text = encounter.Run.FreezeMoves.ToString();
            freezeCount.transform.position = player.position + new Vector3(0f,0f,-0.3f);
            freezeTint.color = new Color(0.38f,0.1f,0.7f,freezeAlpha);
            progress.fillAmount = encounter.Run.Phase == EncounterPhase.Boss || encounter.Run.IsInfinite ? 0f : encounter.Run.Resolved/(float)RunState.EnemiesPerChapter;
            personalShield.enabled = encounter.Run.PersonalShield && controls.GameplayInputEnabled;
            personalShield.useWorldSpace = true; personalShield.positionCount = 49;
            for (int i = 0; i < 49; i++)
            {
                float angle = i/48f*Mathf.PI*2f;
                personalShield.SetPosition(i,player.position + new Vector3(Mathf.Cos(angle)*0.49f,Mathf.Sin(angle)*0.49f,-0.15f));
            }
            if (hitRemaining > 0f)
            {
                hitRemaining -= Time.unscaledDeltaTime;
                properties.SetColor("_BaseColor",new Color(1f,0.15f,0.25f)); properties.SetColor("_Color",new Color(1f,0.15f,0.25f));
                playerRenderer.SetPropertyBlock(hitRemaining > 0f ? properties : null);
            }
        }
        private void OnDestroy()
        {
            if (encounter != null) { encounter.Run.Notice -= ShowNotice; encounter.PlayerDamaged -= Hit; }
            if (controls != null) { controls.ActionNotice -= ShowNotice; controls.ActionTaken -= PlayNote;controls.ActionUndone-=UndoFeedback; }
            if (notes != null) foreach (AudioClip note in notes) if (note != null) Destroy(note);
            if(undoClip!=null)Destroy(undoClip);
            if(freezeTint!=null)freezeTint.material=originalFreezeMaterial;
            if(freezeMaterial!=null)Destroy(freezeMaterial);
        }
    }
}
