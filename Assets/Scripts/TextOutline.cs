using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// 基于 Mesh 的文字描边效果
    /// </summary>
    [RequireComponent(typeof(Text))]
    public class TextOutline : BaseMeshEffect
    {
        public Color OutlineColor = Color.black;
        public Vector2 OutlineDistance = Vector2.one;

        // 用于关闭恢复描边效果
        Material mCachedMat = null;

        #if UNITY_EDITOR

        protected override void Awake()
        {
            base.Awake();

            if (base.graphic != null && base.graphic.material == base.graphic.defaultMaterial)
            {
                base.graphic.material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/TextOutline.mat");
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            
            if (base.graphic != null && base.graphic.material != base.graphic.defaultMaterial)
            {
                base.graphic.material = null;
            }
        }

        #endif

        protected override void Start()
        {
            base.Start();

            AddShaderChannels();
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            if (mCachedMat != null && base.graphic.material != mCachedMat)
            {
                base.graphic.material = mCachedMat;
            }
        }

        protected override void OnDisable()
        {
            base.OnDisable();

            if (base.graphic.material != base.graphic.defaultMaterial)
            {
                mCachedMat = base.graphic.material;
                base.graphic.material = base.graphic.defaultMaterial;
            }
        }

        public override void ModifyMesh(VertexHelper vh)
        {
            List<UIVertex> verts = new List<UIVertex>();
            vh.GetUIVertexStream(verts);

            ProcessVertices(verts);

            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }

        /// <summary>
        /// 开启shader使用的顶点数据通道，用于传输描边数据
        /// </summary>
        private void AddShaderChannels()
        {
            UnityEngine.Profiling.Profiler.BeginSample("AddShaderChannels");
            if (base.graphic && base.graphic.canvas)
            {
                var v1 = graphic.canvas.additionalShaderChannels;
                var v2 = AdditionalCanvasShaderChannels.TexCoord1;
                if ((v1 & v2) != v2)
                {
                    base.graphic.canvas.additionalShaderChannels |= v2;
                }

                v2 = AdditionalCanvasShaderChannels.TexCoord2;
                if ((v1 & v2) != v2)
                {
                    base.graphic.canvas.additionalShaderChannels |= v2;
                }

                v2 = AdditionalCanvasShaderChannels.TexCoord3;
                if ((v1 & v2) != v2)
                {
                    base.graphic.canvas.additionalShaderChannels |= v2;
                }

                v2 = AdditionalCanvasShaderChannels.Tangent;
                if ((v1 & v2) != v2)
                {
                    base.graphic.canvas.additionalShaderChannels |= v2;
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
        }

        void ProcessVertices(List<UIVertex> verts)
        {
            if (verts == null) return;

            for (int i = 0; i <= verts.Count - 3; i += 3)
            {
                // 3 个顶点组成一个三角面
                UIVertex v1 = verts[i];
                UIVertex v2 = verts[i + 1];
                UIVertex v3 = verts[i + 2];

                // 计算中心点坐标
                Vector2 minPos = _Min(v1.position, v2.position, v3.position);
                Vector2 maxPos = _Max(v1.position, v2.position, v3.position);
                Vector2 centerPos = (maxPos + minPos) * 0.5f;

                // 计算原始uv范围
                Vector2 minUV = _Min(v1.uv0, v2.uv0, v3.uv0);
                Vector2 maxUV = _Max(v1.uv0, v2.uv0, v3.uv0);
                Vector2 centerUV = (minUV + maxUV) * 0.5f;

                // 计算单位距离对应的uv值
                float perU = (maxUV.x - minUV.x) / (maxPos.x - minPos.x);
                float perV = (maxUV.y - minUV.y) / (maxPos.y - minPos.y);

                SetNewVertexInfo(ref v1, centerPos, centerUV, perU, perV, minUV, maxUV);
                SetNewVertexInfo(ref v2, centerPos, centerUV, perU, perV, minUV, maxUV);
                SetNewVertexInfo(ref v3, centerPos, centerUV, perU, perV, minUV, maxUV);

                verts[i] = v1;
                verts[i + 1] = v2;
                verts[i + 2] = v3;
            }
        }

        /// <summary>
        /// 设置新的顶点数据
        /// </summary>
        /// <param name="v"></param>
        /// <param name="centerPos"></param>
        /// <param name="perU"></param>
        /// <param name="perV"></param>
        /// <param name="minUV"></param>
        /// <param name="maxUV"></param>
        void SetNewVertexInfo(ref UIVertex v, Vector2 centerPos, Vector2 centerUV,
            float perU, float perV, Vector2 minUV, Vector2 maxUV)
        {
            // 新顶点坐标
            Vector3 pos = v.position;
            float xoffset = pos.x > centerPos.x ? OutlineDistance.x : -OutlineDistance.x;
            float yOffset = pos.y > centerPos.y ? OutlineDistance.y : -OutlineDistance.y;
            pos.x += xoffset;
            pos.y += yOffset;
            v.position = pos;

            // 新uv
            Vector2 uv = v.uv0;
            xoffset = uv.x > centerUV.x ? OutlineDistance.x : -OutlineDistance.x;
            yOffset = uv.y > centerUV.y ? OutlineDistance.y : -OutlineDistance.y;
            uv.x += perU * xoffset;
            uv.y += perV * yOffset;
            v.uv0 = uv;

            // 原始uv
            v.uv1 = minUV;
            v.uv2 = maxUV;

            // 描边数据
            v.uv3 = OutlineDistance;
            v.tangent = OutlineColor;
        }

        Vector2 _Min(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(Mathf.Min(pA.x, pB.x, pC.x), Mathf.Min(pA.y, pB.y, pC.y));
        }

        Vector2 _Max(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(Mathf.Max(pA.x, pB.x, pC.x), Mathf.Max(pA.y, pB.y, pC.y));
        }
    }
}