using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;

namespace FogOfWarNS
{
	public class FogOfWar : MonoBehaviour
    {
        static Mesh s_FullscreenMesh = null;
        static GameObject s_FogRoot;

        /// <summary>
        /// Returns a mesh that you can use with <see cref="CommandBuffer.DrawMesh(Mesh, Matrix4x4, Material)"/> to render full-screen effects.
        /// </summary>
        public static Mesh fullScreenMesh
        {
            get
            {
                if (s_FullscreenMesh != null)
                    return s_FullscreenMesh;

                float topV = 1.0f;
                float bottomV = 0.0f;

                s_FullscreenMesh = new Mesh { name = "Fullscreen Quad" };
                s_FullscreenMesh.SetVertices(new List<Vector3>
                {
                    new Vector3(-1.0f, -1.0f, 0.0f),
                    new Vector3(-1.0f,  1.0f, 0.0f),
                    new Vector3(1.0f, -1.0f, 0.0f),
                    new Vector3(1.0f,  1.0f, 0.0f)
                });

                s_FullscreenMesh.SetUVs(0, new List<Vector2>
                {
                    new Vector2(0.0f, bottomV),
                    new Vector2(0.0f, topV),
                    new Vector2(1.0f, bottomV),
                    new Vector2(1.0f, topV)
                });

                s_FullscreenMesh.SetIndices(new[] { 0, 1, 2, 2, 1, 3 }, MeshTopology.Triangles, 0, false);
                s_FullscreenMesh.UploadMeshData(true);
                return s_FullscreenMesh;
            }
        }

        public const int unitsLimit = 1000;

        CommandBuffer m_CommandBuffer;

        readonly List<FogUnitData> unitsToShowInFOW = new List<FogUnitData>();
		
		private Material m_FogOfWarMaterial;
        private Material m_RecordFogMaterail;
		
		static readonly int maxUnitsId = Shader.PropertyToID("_MaxUnits");
		static readonly int totalUnitsId = Shader.PropertyToID("_ActualUnitsCount");
		static readonly int visionRadiusesTextureId = Shader.PropertyToID("_FOWVisionRadiusesTexture");
		static readonly int positionsTextureId = Shader.PropertyToID("_FOWPositionsTexture");
        static readonly int recordTextureID = Shader.PropertyToID("_RecordTexture");
        static readonly int recordPositionsTextureID = Shader.PropertyToID("_RecordPositionsTexture");
        static readonly int recordVisionRadiusesTextureID = Shader.PropertyToID("_RecordVisionRadiusesTexture");
        
        Texture2D positionsTexture, visionRadiusesTexture;
        Texture2D recordPositionsTexture, recordVisionRadiusesTexture;
        public RenderTexture []m_RecordRTs;//记录行走过的区域;
        private int m_RecordRTsIndex = 0;

        /// <summary>
        /// 记录此区块迷雾左下角起始坐标;
        /// </summary>
        Vector4 startPos;
        bool m_Enabled = false;
        private LayerMask m_LayerMask;
        private bool m_FirstExcuted = true;
        private bool m_Inited = false;
		
		void Awake()
		{
            Vector4 position = transform.position;
            Vector4 scale = transform.lossyScale;
            startPos = new Vector4(position.x - scale.x / 2, position.z - scale.y / 2, scale.x, scale.y);
            m_Enabled = true;
            m_LayerMask = LayerMask.NameToLayer("FogOfWar");
            m_FirstExcuted = true;
        }

        void Start()
		{
            Shader shader = Shader.Find("Hidden/Scene/FogOfWar");
            if (shader == null)
                Debug.LogError("Shader Hidden/Scene/FogOfWar Not Find");
            m_FogOfWarMaterial = new Material(shader);
            
            Shader recordFogShader = Shader.Find("Hidden/Scene/RecordFogInfo");
            if (recordFogShader == null)
                Debug.LogError("Shader Hidden/Scene/RecordFogInfo");
            m_RecordFogMaterail = new Material(recordFogShader);

            CreateTextures();
            CreateRTs();
            
            m_CommandBuffer = new CommandBuffer();
            m_CommandBuffer.name = "FogOfWar"+this.gameObject.GetHashCode();

        }

        private void CreateTextures()
        {
            positionsTexture = new Texture2D(unitsLimit, 1, TextureFormat.RGBAFloat, false, true);
            positionsTexture.wrapMode = TextureWrapMode.Clamp;
            positionsTexture.filterMode = FilterMode.Point;

            visionRadiusesTexture = new Texture2D(unitsLimit, 1, TextureFormat.RFloat, false, true);
            visionRadiusesTexture.wrapMode = TextureWrapMode.Clamp;
            visionRadiusesTexture.filterMode = FilterMode.Point;

            recordPositionsTexture = new Texture2D(unitsLimit, 1, TextureFormat.RGBAFloat, false, true);
            recordPositionsTexture.wrapMode = TextureWrapMode.Clamp;
            recordPositionsTexture.filterMode = FilterMode.Point;

            recordVisionRadiusesTexture = new Texture2D(unitsLimit, 1, TextureFormat.RFloat, false, true);
            recordVisionRadiusesTexture.wrapMode = TextureWrapMode.Clamp;
            recordVisionRadiusesTexture.filterMode = FilterMode.Point;
        }

        private void CreateRTs()
        {
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor();
            renderTextureDescriptor.autoGenerateMips = false;
            renderTextureDescriptor.depthBufferBits = 16;
            renderTextureDescriptor.volumeDepth = 1;
            renderTextureDescriptor.msaaSamples = 2;
            renderTextureDescriptor.dimension = TextureDimension.Tex2D;
            renderTextureDescriptor.graphicsFormat = GraphicsFormat.R8_UNorm;
            renderTextureDescriptor.colorFormat = RenderTextureFormat.R8;
            renderTextureDescriptor.vrUsage = VRTextureUsage.None;
            renderTextureDescriptor.enableRandomWrite = false;         
            
            float ratio = transform.lossyScale.x / transform.lossyScale.y;
            renderTextureDescriptor.width = 1024;
            renderTextureDescriptor.height = (int)(1024*ratio+0.5);

            if (renderTextureDescriptor.height > 2048)
            {
                renderTextureDescriptor.height = 2048;
                renderTextureDescriptor.width = (int)(2048 / ratio+0.5);
            }

            m_RecordRTs = new RenderTexture[2];
            m_RecordRTs[0] = RenderTexture.GetTemporary(renderTextureDescriptor);
            m_RecordRTs[0].name = "RT_0";
            m_RecordRTs[1] = RenderTexture.GetTemporary(renderTextureDescriptor);
            m_RecordRTs[1].name = "RT_1";
            m_RecordRTsIndex = 0;
        }

        void Init()
        {
            if (m_Inited) return;
            FogOfWarManager.Instance.AddFogOfWar(this);
            Camera camera = FogOfWarManager.Instance.GetComponent<Camera>();
            camera.AddCommandBuffer(CameraEvent.BeforeImageEffects, m_CommandBuffer);
            m_Inited = true;
        }

        void Update()
		{
            if (!m_Enabled || m_CommandBuffer == null) return;
            if (FogOfWarManager.Instance == null)
                return;
            Init();
            
            RecalculateUnitsVisibilityInFOW();
            DrawToScreen();

            if (RecalculateUnitsVisibilityRecord())
            {
                DrawRecordFog();
            }
        }

        private void DrawToScreen()
        {
            int readIndex = m_RecordRTsIndex % 2;

            m_CommandBuffer.Clear();
            Camera camera = FogOfWarManager.Instance.GetComponent<Camera>();

            m_FogOfWarMaterial.SetVector("_StartPos", startPos);
            m_FogOfWarMaterial.SetTexture(recordTextureID, m_RecordRTs[readIndex]);
            Matrix4x4 matrix = camera.cameraToWorldMatrix;
            matrix.m02 = -matrix.m02;
            matrix.m12 = -matrix.m12;
            matrix.m22 = -matrix.m22;
            matrix.m32 = -matrix.m32;
            m_CommandBuffer.SetGlobalMatrix("_CCameraToWorld", matrix);
            m_CommandBuffer.SetGlobalMatrix("_CCameraInvProjection", camera.projectionMatrix.inverse);
            m_CommandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.identity, false));
            m_CommandBuffer.DrawMesh(fullScreenMesh, Matrix4x4.identity, m_FogOfWarMaterial, 0, 0);
        }

        private void DrawRecordFog()
        {
            int readIndex = m_RecordRTsIndex % 2;
            int writeIndex = (++m_RecordRTsIndex) % 2;
            m_RecordRTsIndex %= 2;
            Camera camera = FogOfWarManager.Instance.GetComponent<Camera>();
            RecordFogInfo(ref camera, ref readIndex, ref writeIndex);
        }

        /// <summary>
        /// 记录迷雾整个区域信息;
        /// </summary>
        /// <param name="camera"></param>
        /// <param name="readIndex"></param>
        /// <param name="writeIndex"></param>
        void RecordFogInfo(ref Camera camera, ref int readIndex, ref int writeIndex)
        {
            m_RecordFogMaterail.SetVector("_StartPos", startPos);
            if (!m_FirstExcuted)
            {
                m_RecordFogMaterail.SetTexture(recordTextureID, m_RecordRTs[readIndex]);
            }

            m_CommandBuffer.SetViewProjectionMatrices(Matrix4x4.identity, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));
            m_CommandBuffer.SetViewport(new Rect(0, 0, m_RecordRTs[writeIndex].width, m_RecordRTs[writeIndex].height));
            m_CommandBuffer.SetRenderTarget(m_RecordRTs[writeIndex]);
            if (m_FirstExcuted)
            {
                m_CommandBuffer.ClearRenderTarget(true, true, Color.black);
            }
            m_FirstExcuted = false;

            m_CommandBuffer.DrawMesh(fullScreenMesh, Matrix4x4.identity, m_RecordFogMaterail);

            m_CommandBuffer.SetRenderTarget(new RenderTargetIdentifier(BuiltinRenderTextureType.None));
            m_CommandBuffer.SetViewProjectionMatrices(camera.worldToCameraMatrix, camera.projectionMatrix);
            //cmd.Blit(m_RecordRTs[readIndex], m_RecordRTs[writeIndex], recordFogMaterail);
        }

        private void ResetTexture(ref Texture2D tex)
        {
            if (tex != null)
            {
                int width = unitsToShowInFOW.Count;
                for (int i=0; i<width; ++i)
                {
                    tex.SetPixel(i, 0, Color.black);
                }
                tex.Apply();
            }
        }

        /// <summary>
        /// 记录位置发生变化的;
        /// </summary>
        bool RecalculateUnitsVisibilityRecord()
        {
            bool success = false;
            ResetTexture(ref recordPositionsTexture);
            ResetTexture(ref recordVisionRadiusesTexture);
            m_RecordFogMaterail.SetFloat(totalUnitsId, 0);

            int cnt = 0;
            for (int i = 0; i < unitsToShowInFOW.Count; i++)
            {
                if (i >= unitsLimit)
                    break;

                if (unitsToShowInFOW[i].visionDistance < float.Epsilon) continue;
                if (!unitsToShowInFOW[i].beMoved) continue;

                var pos = unitsToShowInFOW[i].transform.position;
                var positionColor2 = new Vector4((pos.x - startPos.x) / startPos.z, 0, (pos.z - startPos.y) / startPos.w, 1f);
                recordPositionsTexture.SetPixel(cnt, 0, positionColor2);
                recordVisionRadiusesTexture.SetPixel(cnt, 0, new Color(unitsToShowInFOW[i].visionDistance / 100f, 0f, 0f, 0f));
                ++cnt;
                success = true;
            }

            if (!success) return success;

            recordPositionsTexture.Apply();
            recordVisionRadiusesTexture.Apply();

            m_RecordFogMaterail.SetFloat(totalUnitsId, cnt);
            m_RecordFogMaterail.SetTexture(recordVisionRadiusesTextureID, recordVisionRadiusesTexture);
            m_RecordFogMaterail.SetTexture(recordPositionsTextureID, recordPositionsTexture);

            return success;
        }

        void RecalculateUnitsVisibilityInFOW()
		{
            ResetTexture(ref visionRadiusesTexture);
            ResetTexture(ref positionsTexture);

            for (int i = 0; i < unitsToShowInFOW.Count; i++)
			{
				if (i >= unitsLimit)
					break;

                if (unitsToShowInFOW[i].visionDistance < float.Epsilon) continue;

				var pos = unitsToShowInFOW[i].transform.position;
                //Note:这里需要自己做个映射,此demo只是简单处理;
                var positionColor = new Color(pos.x / 1024f, pos.y / 1024f, pos.z / 1024f, 1f);
                positionsTexture.SetPixel(i, 0, positionColor);
                visionRadiusesTexture.SetPixel(i, 0, new Color(unitsToShowInFOW[i].visionDistance / 512f, 0f, 0f, 0f));
			}

            visionRadiusesTexture.Apply();
            positionsTexture.Apply();

            m_FogOfWarMaterial.SetFloat(totalUnitsId, unitsToShowInFOW.Count);
            m_FogOfWarMaterial.SetTexture(visionRadiusesTextureId, visionRadiusesTexture);
            m_FogOfWarMaterial.SetTexture(positionsTextureId, positionsTexture);
        }

        public void OnUnitSpawned(FogUnitData unit)
		{
            if(unit != null && !unitsToShowInFOW.Contains(unit))
            {
    			unitsToShowInFOW.Add(unit);
            }
		}

		public void OnUnitDestroyed(FogUnitData unit)
		{
            if (unit != null && unitsToShowInFOW.Contains(unit))
            {
			    unitsToShowInFOW.Remove(unit);
            }
		}

        public void SwichFog()
        {
            if (m_Enabled)
            {
                Camera camera = FogOfWarManager.Instance.GetComponent<Camera>();
                camera.RemoveAllCommandBuffers();
            }
            m_Enabled = !m_Enabled;
        }

		void OnDestroy()
		{
            FogOfWarManager.Instance.DeleteFogOfWar(this);
            if (m_CommandBuffer != null) m_CommandBuffer.Dispose();
            if (m_RecordRTs != null)
            {
                RenderTexture.ReleaseTemporary(m_RecordRTs[0]);
                RenderTexture.ReleaseTemporary(m_RecordRTs[1]);
            }
		}
	}
}