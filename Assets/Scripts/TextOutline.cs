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
                base.graphic.material = UnityEditor.AssetDatabase.LoadAssetAtPath<Material>("Materials/TextOutline.mat");
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
                Vector2 center = (maxPos + minPos) * 0.5f;

                // 计算原始uv范围
                Vector2 minUV = _Min(v1.uv0, v2.uv0, v3.uv0);
                Vector2 maxUV = _Max(v1.uv0, v2.uv0, v3.uv0);

                // 计算uv的单位长度
                Vector2 triX, triY, uvX, uvY;
                Vector2 pos1 = v1.position;
                Vector2 pos2 = v2.position;
                Vector2 pos3 = v3.position;
                if (Mathf.Abs(Vector2.Dot((pos2 - pos1).normalized, Vector2.right))
                    > Mathf.Abs(Vector2.Dot((pos3 - pos2).normalized, Vector2.right)))
                {
                    triX = pos2 - pos1;
                    triY = pos3 - pos2;
                    uvX = v2.uv0 - v1.uv0;
                    uvY = v3.uv0 - v2.uv0;
                }
                else
                {
                    triX = pos3 - pos2;
                    triY = pos2 - pos1;
                    uvX = v3.uv0 - v2.uv0;
                    uvY = v2.uv0 - v1.uv0;
                }
                Vector2 perU = uvX / triX.magnitude * (Vector2.Dot(triX, Vector2.right) > 0 ? 1 : -1);
                Vector2 perV = uvY / triY.magnitude * (Vector2.Dot(triY, Vector2.up) > 0 ? 1 : -1);

                SetNewVertexInfo(ref v1, center, perU, perV, minUV, maxUV);
                SetNewVertexInfo(ref v2, center, perU, perV, minUV, maxUV);
                SetNewVertexInfo(ref v3, center, perU, perV, minUV, maxUV);

                verts[i] = v1;
                verts[i + 1] = v2;
                verts[i + 2] = v3;
            }
        }

        /// <summary>
        /// 设置新的顶点数据
        /// </summary>
        /// <param name="v"></param>
        /// <param name="center"></param>
        /// <param name="perU"></param>
        /// <param name="perV"></param>
        /// <param name="minUV"></param>
        /// <param name="maxUV"></param>
        void SetNewVertexInfo(ref UIVertex v, Vector2 center,
            Vector2 perU, Vector2 perV, Vector2 minUV, Vector2 maxUV)
        {
            // Position
            var pos = v.position;
            var posXOffset = pos.x > center.x ? OutlineDistance.x : -OutlineDistance.x;
            var posYOffset = pos.y > center.y ? OutlineDistance.y : -OutlineDistance.y;
            pos.x += posXOffset;
            pos.y += posYOffset;
            v.position = pos;
            // UV
            var uv = v.uv0;
            uv += perU * posXOffset;
            uv += perV * posYOffset;
            v.uv0 = uv;

            v.uv1 = minUV; //uv1 uv2 可用  tangent  normal 在缩放情况 会有问题
            v.uv2 = maxUV;

            v.uv3 = OutlineDistance;
            v.tangent = OutlineColor;
        }

        Vector2 _Min(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Min(pA.x, pB.x, pC.x), _Min(pA.y, pB.y, pC.y));
        }

        float _Min(float pA, float pB, float pC)
        {
            return Mathf.Min(Mathf.Min(pA, pB), pC);
        }

        Vector2 _Max(Vector2 pA, Vector2 pB, Vector2 pC)
        {
            return new Vector2(_Max(pA.x, pB.x, pC.x), _Max(pA.y, pB.y, pC.y));
        }

        float _Max(float pA, float pB, float pC)
        {
            return Mathf.Max(Mathf.Max(pA, pB), pC);
        }
    }
}