using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FogOfWarNS
{
    public class FogOfWarManager : MonoBehaviour
    {
        static FogOfWarManager s_FogOfWarManagerInstance;
        static Dictionary<int, FogOfWar> s_FogOfWarDic = new Dictionary<int, FogOfWar>();

        private CommandBuffer m_CommandBuffer;

        static int s_FogStrengthID = Shader.PropertyToID("_FogStrength");
        static readonly int unexploredID = Shader.PropertyToID("_UnExploredColor");
        static readonly int exploredID = Shader.PropertyToID("_ExploredColor");

        /// <summary>
        /// 未探索过的区域颜色;
        /// </summary>
        private Color m_UnexploredColor = Color.black;
        public Color unexploredColor
        {
            get
            {
                return m_UnexploredColor;
            }
            set
            {
                if (m_UnexploredColor != value)
                {
                    m_UnexploredColor = value;
                    Shader.SetGlobalColor(unexploredID, m_UnexploredColor);
                }
            }
        }

        /// <summary>
        /// 已经探索过的区域颜色;
        /// </summary>
        private Color m_ExploredColor = new Color(0, 0, 0, 0.5f);
        public Color exploredColor
        {
            get { return m_ExploredColor;  }
            set
            {
                if (m_ExploredColor != value)
                {
                    m_ExploredColor = value;
                    Shader.SetGlobalColor(exploredID, m_ExploredColor);
                }
            }
        }

        public float updateTime = 0.01f;
        public static float fogStrength = 0.8f;
        private static Camera m_Camera;

        public static FogOfWarManager Instance
        {
            get
            {
                if (s_FogOfWarManagerInstance == null)
                {
                    Debug.LogWarning("FogOfWarManager is null");
                }
                return s_FogOfWarManagerInstance;
            }
        }

        private RenderTexture m_CameraColorRT;
        private RenderTexture m_CameraDepthRT;
        private RenderTexture m_DepthRT;

        private void Awake()
        {
            if (s_FogOfWarManagerInstance != null)
            {
                Debug.LogError("FogOfWarManager is singleton");
            }
            s_FogOfWarManagerInstance = this;
            m_Camera = GetComponent<Camera>();
            CreateRTs();

            m_Camera.SetTargetBuffers(m_CameraColorRT.colorBuffer, m_CameraDepthRT.depthBuffer);
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "GetCameraDepthTexture";
            m_CommandBuffer.Blit(m_CameraDepthRT.depthBuffer, m_DepthRT);
            m_Camera.AddCommandBuffer(CameraEvent.AfterSkybox, m_CommandBuffer);
        }

        private void CreateRTs()
        {
            m_CameraColorRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, 
                RenderTextureFormat.RGB111110Float);
            m_CameraColorRT.name = "CameraTargetColor";

            m_CameraDepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 24,
                RenderTextureFormat.Depth);
            m_CameraDepthRT.name = "CameraTargetDepth";

            m_DepthRT = RenderTexture.GetTemporary(m_Camera.pixelWidth, m_Camera.pixelHeight, 0, 
                RenderTextureFormat.RHalf);
            m_DepthRT.name = "DepthColorTex";   
        }

        private void Update()
        {
            Shader.SetGlobalTexture("_CDepthTexture", m_DepthRT);
#if UNITY_EDITOR
            Shader.SetGlobalFloat(s_FogStrengthID, fogStrength);
#endif
        }

        private void OnPostRender()
        {
            Graphics.Blit(m_CameraColorRT, (RenderTexture)null);
        }

        private void Clear()
        {
            if (this.m_CommandBuffer != null)
            {
                this.m_CommandBuffer.Clear();
                m_Camera.RemoveCommandBuffer(CameraEvent.BeforeImageEffects, this.m_CommandBuffer);
                this.m_CommandBuffer.Dispose();
                this.m_CommandBuffer = null;
            }
            if (m_CameraColorRT != null)
            {
                RenderTexture.ReleaseTemporary(m_CameraColorRT);
                m_CameraColorRT = null;
            }
            if (m_CameraDepthRT != null)
            {
                RenderTexture.ReleaseTemporary(m_CameraDepthRT);
                m_CameraDepthRT = null;
            }
            if (m_DepthRT != null)
            {
                RenderTexture.ReleaseTemporary(m_DepthRT);
                m_DepthRT = null;
            }
        }

        private void OnDestroy()
        {
            this.Clear();
        }

        private void OnDisable()
        {
            this.Clear();
        }

        public void SwitchFog()
        {
            using (Dictionary<int, FogOfWar>.Enumerator itor = s_FogOfWarDic.GetEnumerator())
            {
                while (itor.MoveNext())
                {
                    itor.Current.Value.SwichFog();
                }
                itor.Dispose();
            }
        }

        /// <summary>
        /// Add Terrain-fog-instance to manager; 
        /// </summary>
        public void AddFogOfWar(FogOfWar fogOfWar)
        {
            if (fogOfWar != null)
            {
                s_FogOfWarDic.Add(fogOfWar.gameObject.GetHashCode(), 
                    fogOfWar);
            }
        }

        /// <summary>
        /// Delete Terrain-fog-instance from manager; 
        /// </summary>
        public void DeleteFogOfWar(FogOfWar fogOfWar)
        {
            if (fogOfWar != null)
            {
                s_FogOfWarDic.Remove(fogOfWar.gameObject.GetHashCode());
            }
        }

        //Note:强制删除一个空FogOfWar Object对象;防止OnDestroy中不能正确删除;
        private void DeleteEmptyFogObject()
        {
            using (Dictionary<int, FogOfWar>.Enumerator itor = s_FogOfWarDic.GetEnumerator())
            {
                while (itor.MoveNext())
                {
                    if (itor.Current.Value == null)
                    {
                        s_FogOfWarDic.Remove(itor.Current.Key);
                        break;
                    }
                }
                itor.Dispose();
            }
        }

        /// <summary>
        /// 单位进入迷雾区块;
        /// </summary>
        /// <param name="fogObj">迷雾区块GameObject</param>
        /// <param name="unit">迷雾单位数据</param>
        public void AddFogUnit(GameObject fogObj, FogUnitData unit)
        {
            if (fogObj == null)
            {
                Debug.LogError( string.Format("AddFogUnit Failed, Position: {0}", unit.transform.position));
                return;
            }

            int hashCode = fogObj.GetHashCode();
            FogOfWar FogOfWar;
            if (s_FogOfWarDic.TryGetValue(hashCode, out FogOfWar) && FogOfWar != null)
            {
                FogOfWar.OnUnitSpawned(unit);
            }
        }

        /// <summary>
        /// 单位离开迷雾区块;
        /// </summary>
        /// <param name="fogObj">迷雾区块GameObject</param>
        /// <param name="unit">迷雾单位数据</param>
        public void DeleteFogUnit(GameObject fogObj, FogUnitData unit)
        {
            if (fogObj == null)
            {
                Debug.LogWarning(string.Format("DeleteFogUnit Failed, Position: {0}", unit.name));
                return;
            }

            int hashCode = fogObj.GetHashCode();
            FogOfWar FogOfWar;
            if (s_FogOfWarDic.TryGetValue(hashCode, out FogOfWar) && FogOfWar != null)
            {
                FogOfWar.OnUnitDestroyed(unit);
            }
#if UNITY_EDITOR
            else
            {
                Debug.LogError(string.Format("FogOfWar Not Find"));
            }
#endif
        }
    }
}


