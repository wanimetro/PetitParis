using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 이야기 구역: 쥐 몸 중심이 처음 들어오면 손목 패널에 한 줄 메시지와 짧은 소리를 낸다 (한 번만).
    /// 예) 센강이 보이는 곳, 비스트로 주방 입구.
    /// </summary>
    [DisallowMultipleComponent]
    public class RatStoryTrigger : MonoBehaviour
    {
        [SerializeField] RatDangerDetector danger;
        [SerializeField] PetitParisGameFlow gameFlow;
        [SerializeField] Collider volume;
        [SerializeField, TextArea] string message;
        [SerializeField] bool playChime = true;
        [SerializeField] AudioSource sfx;

        bool fired;

        void Awake()
        {
            if (danger == null) danger = FindAnyObjectByType<RatDangerDetector>();
            if (gameFlow == null) gameFlow = FindAnyObjectByType<PetitParisGameFlow>();
            if (volume == null) volume = GetComponent<Collider>();
            if (volume != null) volume.isTrigger = true;
            if (sfx != null) PetitParisAudio.Configure2D(sfx);
        }

        void Update()
        {
            if (fired || danger == null || volume == null) return;
            if (!RatSafeZone.ColliderContains(volume, danger.BodyCenter)) return;
            fired = true;
            if (gameFlow != null && !string.IsNullOrEmpty(message)) gameFlow.PostMessage(message);
            if (playChime && sfx != null) sfx.PlayOneShot(PetitParisAudio.Chime, 0.4f);
        }
    }
}
