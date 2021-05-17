using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FogOfWarNS
{
    public class FogUnitData : MonoBehaviour
    {
        [Tooltip("radius")]
        public float visionDistance;

        private List<GameObject> m_CurrentFogOfWar;
        private List<GameObject> m_LastFogOfWar;

        public Vector2 offset = new Vector2(0.5f, 0.5f);
        private int m_LayerMask = 0;

        /// <summary>
        /// Note:在屏幕里的迷雾做透视处理;
        /// </summary>
        public bool needPersperctive = false;
        public float updateTime = 0.2f;
        private float updateTimer = 0;
        private Vector3 m_Position = Vector3.zero;
        public bool beMoved
        {
            get
            {
                Vector3 position = transform.position;
                bool ret = true;
                if (Math.Abs(position.x - m_Position.x) < float.Epsilon &&
                    Math.Abs(position.y - m_Position.y) < float.Epsilon &&
                    Math.Abs(position.z - m_Position.z) < float.Epsilon
                    )
                {
                    ret = false;
                }
                m_Position = position;
                return ret;
            }
        }
        
        private void Awake()
        {
            m_LayerMask = LayerMask.NameToLayer("FogOfWar");
            m_CurrentFogOfWar = new List<GameObject>();
            m_LastFogOfWar = new List<GameObject>();
        }

        private void OnEnable()
        {
            m_Position = Vector3.zero;
        }

        private void OnDisable()
        {
            if (m_CurrentFogOfWar != null && m_CurrentFogOfWar.Count > 0)
            {
                foreach(GameObject fogOfWar in m_CurrentFogOfWar)
                {
                    if (fogOfWar != null)
                    {
                        FogOfWarManager.Instance.DeleteFogUnit(fogOfWar, this);
                    }
                }
            }
            m_CurrentFogOfWar.Clear();
            m_LastFogOfWar.Clear();
        }

        private void LateUpdate()
        {
            if (FogOfWarManager.Instance == null)
                return;
            if (updateTimer > 0)
            {
                updateTimer -= Time.deltaTime;
                return;
            }

            updateTimer = updateTime;
            RefreshFogOfWar();
        }

        /// <summary>
        /// 更新单位4个方向所在的fog区块;
        /// </summary>
        private void RefreshFogOfWar()
        {
            m_CurrentFogOfWar.Clear();
            Ray ray = new Ray();
            ray.origin = this.transform.position + Vector3.up * 100;

            ray.direction = (this.transform.position + Vector3.back * visionDistance) - ray.origin;
            ray.direction = ray.direction.normalized;
            RegisterFogOfWarFromRayHit(in ray);

            ray.direction = (this.transform.position + Vector3.forward * visionDistance) - ray.origin;
            ray.direction = ray.direction.normalized;
            RegisterFogOfWarFromRayHit(in ray);

            ray.direction = (this.transform.position + Vector3.left * visionDistance) - ray.origin;
            ray.direction = ray.direction.normalized;
            RegisterFogOfWarFromRayHit(in ray);

            ray.direction = (this.transform.position + Vector3.right * visionDistance) - ray.origin;
            ray.direction = ray.direction.normalized;
            RegisterFogOfWarFromRayHit(in ray);

            //删除已经离开的迷雾区块;
            foreach (GameObject fogOfWar in m_LastFogOfWar)
            {
                if (fogOfWar != null && !m_CurrentFogOfWar.Contains(fogOfWar))
                {
                    FogOfWarManager.Instance.DeleteFogUnit(fogOfWar, this);
                }
            }

            m_LastFogOfWar.Clear();
            foreach(GameObject gameObject in m_CurrentFogOfWar)
            {
                if(gameObject != null)
                {
                    m_LastFogOfWar.Add(gameObject);
                }
            }
        }

        private bool RegisterFogOfWarFromRayHit(in Ray ray)
        {
            RaycastHit[] hitInfos = Physics.RaycastAll(ray);
            RaycastHit hitInfo = new RaycastHit();

            if (hitInfos != null && hitInfos.Length > 0)
            {
                for (int i = 0, n = hitInfos.Length; i < n; ++i)
                {
                    if (hitInfos[i].transform != null &&
                        hitInfos[i].transform.gameObject.layer == m_LayerMask)
                    {
                        hitInfo = hitInfos[i];
                        break;
                    }
                }
                if (hitInfo.transform == null) return false;

                GameObject gameObject = hitInfo.transform.gameObject;
                if (gameObject != null)
                {
                    FogOfWarManager.Instance.AddFogUnit(gameObject, this);
                    if (!m_CurrentFogOfWar.Contains(gameObject))
                    {
                        m_CurrentFogOfWar.Add(gameObject);
                    }
                    return true;
                }
            }
            return false;
        }
    }
}
