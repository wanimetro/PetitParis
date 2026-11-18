using UnityEngine;

namespace PetitParis
{
    /// <summary>센강을 천천히 지나가는 유람선/바지선 (원경 장식). x 범위를 벗어나면 반대편에서 다시 들어온다.</summary>
    public class SeineBarge : MonoBehaviour
    {
        [SerializeField] float speed = 0.5f;
        [SerializeField] float xMin = -40f;
        [SerializeField] float xMax = 40f;

        void Update()
        {
            var p = transform.position;
            p.x += speed * Time.deltaTime;
            if (speed > 0f && p.x > xMax) p.x = xMin;
            else if (speed < 0f && p.x < xMin) p.x = xMax;
            transform.position = p;
        }
    }
}
