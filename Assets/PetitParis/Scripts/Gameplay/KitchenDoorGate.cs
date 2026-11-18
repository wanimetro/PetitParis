using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 비스트로 뒷문. 처음엔 닫혀 있다가, 센강 난간 체크포인트(openAtCheckpoint)에 닿으면 끼익 열린다.
    /// → "센강을 바라본 뒤 주방으로" 순서를 만든다. 문짝은 hinge의 자식 (충돌체 포함, 닫혀 있으면 문틀을 막는다).
    /// 정적 배칭(env)이 아닌 Gameplay 그룹에 있어야 움직인다.
    /// </summary>
    [DisallowMultipleComponent]
    public class KitchenDoorGate : MonoBehaviour
    {
        [SerializeField] Transform hinge;
        [SerializeField] CheckpointManager checkpoints;
        [SerializeField] int openAtCheckpoint = 5;
        [SerializeField] float closedYaw = 90f;
        [SerializeField] float openYaw = 0f;
        [SerializeField, Range(0.2f, 4f)] float swingTime = 1.4f;
        [SerializeField] AudioSource audioSource;

        PetitParisGameFlow flow;
        float t = -1f;

        public bool IsOpen { get; private set; }

        void Awake()
        {
            if (checkpoints == null) checkpoints = FindAnyObjectByType<CheckpointManager>();
            flow = FindAnyObjectByType<PetitParisGameFlow>();
            PetitParisAudio.Configure3D(audioSource, 0.5f, 8f);
            SetYaw(closedYaw);
        }

        void OnEnable() { if (checkpoints != null) checkpoints.CheckpointReached += OnCheckpoint; }
        void OnDisable() { if (checkpoints != null) checkpoints.CheckpointReached -= OnCheckpoint; }

        void Start()
        {
            if (checkpoints != null && checkpoints.HighestReachedIndex >= openAtCheckpoint) Open(false);
        }

        void OnCheckpoint(int index)
        {
            if (index >= openAtCheckpoint) Open(true);
        }

        public void Open(bool announce)
        {
            if (IsOpen) return;
            IsOpen = true;
            t = 0f;
            if (audioSource != null) audioSource.PlayOneShot(PetitParisAudio.Chime, 0.8f);
            if (announce && flow != null) flow.PostMessage(PetitParisText.MessageKitchenDoorOpen, 1);
        }

        void Update()
        {
            if (t < 0f) return;
            t += Time.deltaTime / swingTime;
            SetYaw(Mathf.Lerp(closedYaw, openYaw, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t))));
            if (t >= 1f) t = -1f;
        }

        void SetYaw(float yaw)
        {
            if (hinge != null) hinge.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }
    }
}
