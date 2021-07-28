using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine;
using UnityEngine.Events;

using Unity.VectorGraphics;

[ExecuteInEditMode, RequireComponent(typeof(MeshFilter))]
public class BezierMesh : MonoBehaviour
{
    [SerializeField, ContextMenuItem("load from svg", "LoadSvgFile")] string svgFilePath;
    public List<PathParam> pathParams;
    public bool PathEdited { set => pathEdited = value; }
    [SerializeField] bool pathEdited;
    [SerializeField] bool useMesh;
    Mesh m_mesh;

    [SerializeField] MeshEvent onMeshCreated;
    [SerializeField] SpriteEvent onSpriteCreated;

    private void OnValidate()
    {
        pathEdited = true;
    }
    private void OnDestroy()
    {
        if (m_mesh != null)
            Destroy(m_mesh);
    }
    private void Update()
    {
        if (useMesh && m_mesh == null)
        {
            m_mesh = new Mesh();
            m_mesh.MarkDynamic();
            GetComponent<MeshFilter>().sharedMesh = m_mesh;
            onMeshCreated.Invoke(m_mesh);
            PathEdited = true;
        }
        if (pathEdited)
            GenerateMesh();
    }

    void LoadSvgFile()
    {
        if (!File.Exists(svgFilePath) || Path.GetExtension(svgFilePath).ToLower() != ".svg")
            return;

        SVGParser.SceneInfo sceneInfo;
        using (var stream = new StreamReader(svgFilePath))
            sceneInfo = SVGParser.ImportSVG(stream, ViewportOptions.DontPreserve);
        if (sceneInfo.Scene == null || sceneInfo.Scene.Root == null)
            return;

        var root = sceneInfo.Scene.Root;
        var bounds = VectorUtils.SceneNodeBounds(root);
        var nodes = new List<(SceneNode node, Matrix2D transform)>();
        AddChildrens(root, root.Transform);
        var shapes = nodes.Where(n => n.node.Shapes != null).Select(n => (n.node.Shapes, n.transform));
        var beziers = shapes.SelectMany(ss =>
          ss.Shapes.SelectMany(s =>
          {
              var bs = s.Contours;
              for (var i = 0; i < bs.Length; i++)
                  for (var j = 0; j < bs[i].Segments.Length; j++)
                  {
                      var segment = bs[i].Segments[j];
                      bs[i].Segments[j].P0 = ss.transform * segment.P0 - bounds.center;
                      bs[i].Segments[j].P1 = ss.transform * segment.P1 - bounds.center;
                      bs[i].Segments[j].P2 = ss.transform * segment.P2 - bounds.center;
                  }
              return bs;
          }));

        pathParams = beziers.Select(b =>
        {
            b.Closed = true;//force closed curve
            return new PathParam(b);
        }).ToList();

        void AddChildrens(SceneNode node, Matrix2D worldTransform)
        {
            nodes.Add((node, worldTransform));
            if (node.Children != null)
                node.Children.ForEach(c => AddChildrens(c, worldTransform * c.Transform));
        }
        PathEdited = true;
    }
    [ContextMenu("generate mesh")]
    public void GenerateMesh()
    {
        var shape = new Shape()
        {
            Contours = pathParams.Select(param =>
                new BezierContour()
                {
                    Segments = param.segments.Select(s => new BezierPathSegment()
                    {
                        P0 = s.P0 * param.scale + param.offset,
                        P1 = s.P1 * param.scale + param.offset,
                        P2 = s.P2 * param.scale + param.offset,
                    }).ToArray(),
                    Closed = true
                }).ToArray(),
            PathProps = new PathProperties() { Stroke = new Stroke() { HalfThickness = 0f } },
            Fill = new SolidFill() { Color = Color.white },
        };
        var scene = new Scene()
        {
            Root = new SceneNode()
            {
                Shapes = new List<Shape> { shape }
            }
        };
        var options = MakeOptions();
        var geom = VectorUtils.TessellateScene(scene, options);

        var sprite = VectorUtils.BuildSprite(geom, 1f, VectorUtils.Alignment.Center, Vector2.zero, 128);
        var center = sprite.rect.center;

        if (useMesh && m_mesh != null)
        {
            m_mesh.Clear();
            m_mesh.SetVertices(sprite.vertices.Select(v => (Vector3)(v + center)).ToList());
            m_mesh.SetIndices(sprite.triangles.ToList(), MeshTopology.Triangles, 0);
            m_mesh.RecalculateNormals();
            m_mesh.RecalculateTangents();
            m_mesh.RecalculateBounds();
        }

        PathEdited = false;
        onSpriteCreated.Invoke(sprite);

        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        var spriteToSDF = GetComponentInChildren<SpriteToSDF>();
        if (spriteToSDF != null)
            spriteToSDF.GenerateSDF(sprite);
        if (spriteRenderer != null)
        {
            if (spriteRenderer.sprite != null)
                DestroyImmediate(spriteRenderer.sprite);
            spriteRenderer.sprite = sprite;
            spriteRenderer.transform.localPosition = center;
        }
        else
            DestroyImmediate(sprite);
    }

    static VectorUtils.TessellationOptions MakeOptions(float stepDistance = float.MaxValue)
    {
        var options = new VectorUtils.TessellationOptions()
        {
            StepDistance = stepDistance,
            MaxCordDeviation = 0.05f,
            MaxTanAngleDeviation = 0.05f,
            SamplingStepSize = 0.10f
        };

        return options;
    }

    [System.Serializable]
    public struct PathParam
    {
        public SerializableSegment[] segments;
        public Vector2 scale;
        public Vector2 offset;

        public PathParam(BezierContour bezier)
        {
            segments = bezier.Segments.Select(s => new SerializableSegment { P0 = s.P0, P1 = s.P1, P2 = s.P2 }).ToArray();
            scale = new Vector2(0.01f, -0.01f);
            offset = Vector2.zero;
        }
    }

    [System.Serializable]
    public struct SerializableSegment
    {
        public Vector2 P0;
        public Vector2 P1;
        public Vector2 P2;
    }

    [System.Serializable]
    public class MeshEvent : UnityEvent<Mesh> { }
    [System.Serializable]
    public class SpriteEvent : UnityEvent<Sprite> { }
}
