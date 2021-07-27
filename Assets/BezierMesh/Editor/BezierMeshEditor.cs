using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(BezierMesh))]
public class BezierMeshEditor : Editor
{
    private void OnSceneGUI()
    {
        var bezierMesh = (BezierMesh)target;

        bezierMesh.pathParams.ForEach(pathParam =>
        {
            var transform = bezierMesh.transform;
            var segments = pathParam.segments;
            var scale = pathParam.scale;
            var offset = pathParam.offset;
            var numPoints = segments.Length;
            var controlPoints = segments
            .Select(s => (
                p0: transform.TransformPoint(s.P0 * scale + offset),
                p1: transform.TransformPoint(s.P1 * scale + offset),
                p2: transform.TransformPoint(s.P2 * scale + offset)
                )).ToList();
            for (var i = 0; i < numPoints; i++)
            {
                var prev = (i + numPoints - 1) % numPoints;
                var next = (i + 1) % numPoints;

                var cp0 = controlPoints[i];
                var cp1 = controlPoints[next];

                EditorGUI.BeginChangeCheck();
                Handles.DrawBezier(cp0.p0, cp1.p0, cp0.p1, cp0.p2, Color.white, Texture2D.whiteTexture, 1f);

                Handles.color = Color.cyan;
                Handles.DrawLine(cp0.p0, cp0.p1);
                Handles.DrawLine(cp0.p2, cp1.p0);
                Handles.DrawWireCube(cp0.p0, Vector3.one * 0.1f);

                var pos0 = Handles.PositionHandle(cp0.p0, transform.rotation);
                var pos1 = Handles.PositionHandle(cp0.p1, transform.rotation);
                var pos2 = Handles.PositionHandle(cp0.p2, transform.rotation);
                if (EditorGUI.EndChangeCheck())
                {
                    bezierMesh.PathEdited = true;
                    Undo.RecordObject(bezierMesh, "edit bezier");
                    if (cp0.p0 != pos0)
                    {
                        var newPos = ((Vector2)transform.InverseTransformPoint(pos0) - offset) / scale;
                        var delta = newPos - pathParam.segments[i].P0;
                        segments[i].P0 += delta;
                        segments[i].P1 += delta;
                        segments[prev].P2 += delta;
                    }
                    if (cp0.p1 != pos1)
                    {
                        var newPos = ((Vector2)transform.InverseTransformPoint(pos1) - offset) / scale;
                        var angle = Vector3.Angle(segments[i].P0 - segments[i].P1, segments[prev].P2 - segments[i].P0);
                        segments[i].P1 = newPos;
                        if (angle < 5f)
                        {
                            var dir = (segments[i].P0 - newPos).normalized;
                            var length = (segments[prev].P2 - segments[i].P0).magnitude;
                            segments[prev].P2 = segments[i].P0 + dir * length;
                        }
                    }
                    if (cp0.p2 != pos2)
                    {
                        var newPos = ((Vector2)transform.InverseTransformPoint(pos2) - offset) / scale;
                        var angle = Vector3.Angle(segments[next].P0 - segments[i].P2, segments[next].P1 - segments[next].P0);
                        segments[i].P2 = newPos;
                        if (angle < 5f)
                        {
                            var dir = (segments[next].P0 - newPos).normalized;
                            var length = (segments[next].P1 - segments[next].P0).magnitude;
                            segments[next].P1 = segments[next].P0 + dir * length;
                        }
                    }
                    EditorUtility.SetDirty(bezierMesh);
                }
            }
        });
    }
}
