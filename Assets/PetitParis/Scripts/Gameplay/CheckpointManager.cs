using System;
using UnityEngine;

namespace PetitParis
{
    /// <summary>
    /// 체크포인트 목록 관리.
    /// - 쥐가 체크포인트 반경 안에 들어오면 활성화 (소리 + 표시 색 변경)
    /// - 실패하면 "활성화된 체크포인트 중 실패 위치에서 가장 가까운 곳"에서 재시작
    /// - 첫 번째 체크포인트(시작 지점)는 처음부터 활성
    /// - NextTarget: 아직 도달하지 않은 다음 체크포인트 (목표 표시에 사용)
    /// 체크포인트 Transform의 위치 = 재시작 위치(바닥), 파란 Z축 = 재시작 시 바라볼 방향.
    /// </summary>
    [DisallowMultipleComponent]
    public class CheckpointManager : MonoBehaviour
    {
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        [SerializeField] Transform[] checkpoints = new Transform[0];

        [Tooltip("활성화 반경 (월드 m, 수평 거리).")]
        [SerializeField, Range(0.05f, 1f)] float activationRadius = 0.3f;

        [SerializeField] RatScaleRig rig;
        [SerializeField] AudioSource chimeSource;

        [Tooltip("체크포인트마다 색을 바꿀 표시(선택). 인덱스는 checkpoints와 같게.")]
        [SerializeField] Renderer[] markers = new Renderer[0];
        [SerializeField] Color inactiveColor = new Color(0.55f, 0.55f, 0.6f);
        [SerializeField] Color activeColor = new Color(1f, 0.8f, 0.25f);

        bool[] reached;
        MaterialPropertyBlock block;

        public int Count => checkpoints != null ? checkpoints.Length : 0;
        public int ReachedCount { get; private set; }
        public int LastReachedIndex { get; private set; }

        /// <summary>새 체크포인트 활성화 (인덱스).</summary>
        public event Action<int> CheckpointReached;

        void Awake()
        {
            if (rig == null) rig = FindAnyObjectByType<RatScaleRig>();
            if (chimeSource != null) PetitParisAudio.Configure2D(chimeSource);
            block = new MaterialPropertyBlock();
            ResetAll();
        }

        public void ResetAll()
        {
            reached = new bool[Count];
            ReachedCount = 0;
            LastReachedIndex = 0;
            if (Count > 0)
            {
                reached[0] = true;
                ReachedCount = 1;
            }
            for (int i = 0; i < Count; i++) Paint(i);
        }

        void Update()
        {
            if (rig == null || reached == null) return;
            Vector3 p = rig.transform.position;
            for (int i = 0; i < Count; i++)
            {
                if (reached[i] || checkpoints[i] == null) continue;
                Vector3 c = checkpoints[i].position;
                Vector2 d = new Vector2(p.x - c.x, p.z - c.z);
                if (d.sqrMagnitude <= activationRadius * activationRadius && Mathf.Abs(p.y - c.y) < 0.3f)
                    Reach(i);
            }
        }

        void Reach(int index)
        {
            reached[index] = true;
            ReachedCount++;
            LastReachedIndex = index;
            Paint(index);
            if (chimeSource != null) chimeSource.PlayOneShot(PetitParisAudio.Chime, 0.6f);
            CheckpointReached?.Invoke(index);
        }

        void Paint(int index)
        {
            if (markers == null || index >= markers.Length || markers[index] == null) return;
            markers[index].GetPropertyBlock(block);
            block.SetColor(BaseColorId, reached != null && reached[index] ? activeColor : inactiveColor);
            markers[index].SetPropertyBlock(block);
        }

        /// <summary>활성화된 체크포인트 중 from에서 가장 가까운 곳. 없으면 첫 번째.</summary>
        public Transform GetRespawnPoint(Vector3 from)
        {
            Transform best = null;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < Count; i++)
            {
                if (checkpoints[i] == null || reached == null || !reached[i]) continue;
                float d = (checkpoints[i].position - from).sqrMagnitude;
                if (d < bestDistance) { bestDistance = d; best = checkpoints[i]; }
            }
            if (best == null && Count > 0) best = checkpoints[0];
            return best;
        }

        /// <summary>아직 도달하지 않은 다음 체크포인트 (순서대로). 모두 도달했으면 null.</summary>
        public Transform NextTarget
        {
            get
            {
                if (reached == null) return null;
                for (int i = 0; i < Count; i++)
                    if (!reached[i] && checkpoints[i] != null) return checkpoints[i];
                return null;
            }
        }

        void OnDrawGizmos()
        {
            if (checkpoints == null) return;
            Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.8f);
            foreach (var c in checkpoints)
            {
                if (c == null) continue;
                Gizmos.DrawWireSphere(c.position, activationRadius);
                Gizmos.DrawLine(c.position, c.position + c.forward * 0.2f);
            }
        }
    }
}
