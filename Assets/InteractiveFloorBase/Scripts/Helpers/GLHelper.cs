using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;
using static Unity.Mathematics.math;

/// <summary>
/// 
/// </summary>
public static class GLHelper
{
    /// <summary>
    /// GL関数を用いて、XY平面上にサークルを描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="radius">半径</param>
    /// <param name="col">色</param>
    public static void DrawGLCircleXY(Vector3 center, float radius, Color? col)
    {
        var circleRes = 24;
        var dAng = (2.0f * PI) / (circleRes - 1);

        GL.Begin(GL.TRIANGLE_STRIP);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        for (var i = 0; i < circleRes; i++)
        {
            var ang0 = dAng * i;
            var ang1 = dAng * (i + 1);
            var x0 = radius * cos(ang0);
            var y0 = radius * sin(ang0);
            var x1 = radius * cos(ang1);
            var y1 = radius * sin(ang1);
            GL.Vertex3(center.x     , center.y,      center.z);
            GL.Vertex3(center.x + x0, center.y + y0, center.z);
            GL.Vertex3(center.x + x1, center.y + y1, center.z);
        }
        GL.End();
    }

    /// <summary>
    /// GL描画関数を用いて、XZ平面上にサークルを描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="radius">半径</param>
    /// <param name="col">色</param>
    public static void DrawGLCircleXZ(Vector3 center, float radius, Color? col)
    {
        var circleRes = 24;
        var dAng = (2.0f * PI) / (circleRes - 1);

        GL.Begin(GL.TRIANGLE_STRIP);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        for (var i = 0; i < circleRes; i++)
        {
            var ang0 = dAng * i;
            var ang1 = dAng * (i + 1);
            var x0 = radius * cos(ang0);
            var z0 = radius * sin(ang0);
            var x1 = radius * cos(ang1);
            var z1 = radius * sin(ang1);
            GL.Vertex3(center.x,      center.y, center.z     );
            GL.Vertex3(center.x + x0, center.y, center.z + z0);
            GL.Vertex3(center.x + x1, center.y, center.z + z1);
        }
        GL.End();
    }

    /// <summary>
    /// GL描画関数を用いて、XY平面上に矩形を描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="col">色</param>
    public static void DrawGLRectXY(Vector3 center, float width, float height, Color? col)
    {
        GL.Begin(GL.QUADS);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        GL.Vertex3(center.x - width * 0.5f, center.y - height * 0.5f, center.z);
        GL.Vertex3(center.x - width * 0.5f, center.y + height * 0.5f, center.z);
        GL.Vertex3(center.x + width * 0.5f, center.y + height * 0.5f, center.z);
        GL.Vertex3(center.x + width * 0.5f, center.y - height * 0.5f, center.z);

        GL.End();
    }

    /// <summary>
    /// GL関数を用いて、XZ平面上に矩形を描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="col">色</param>
    public static void DrawGLRectXZ(Vector3 center, float width, float height, Color? col)
    {
        GL.Begin(GL.QUADS);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        GL.Vertex3(center.x - width * 0.5f, center.y, center.z - height * 0.5f);
        GL.Vertex3(center.x - width * 0.5f, center.y, center.z + height * 0.5f);
        GL.Vertex3(center.x + width * 0.5f, center.y, center.z + height * 0.5f);
        GL.Vertex3(center.x + width * 0.5f, center.y, center.z - height * 0.5f);

        GL.End();
    }

    /// <summary>
    /// GL関数を用いて、XY平面上にワイヤーフレームで矩形を描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="col">色</param>
    public static void DrawGLWireRectXY(Vector3 center, float width, float height, Color? col)
    {
        GL.Begin(GL.LINE_STRIP);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        GL.Vertex3(center.x - width * 0.5f, center.y - height * 0.5f, center.z);
        GL.Vertex3(center.x - width * 0.5f, center.y + height * 0.5f, center.z);
        GL.Vertex3(center.x + width * 0.5f, center.y + height * 0.5f, center.z);
        GL.Vertex3(center.x + width * 0.5f, center.y - height * 0.5f, center.z);
        GL.Vertex3(center.x - width * 0.5f, center.y - height * 0.5f, center.z);


        GL.End();
    }

    /// <summary>
    /// GL関数を用いて、XZ平面上にワイヤーフレームで矩形を描画
    /// </summary>
    /// <param name="center">中心座標</param>
    /// <param name="width">幅</param>
    /// <param name="height">高さ</param>
    /// <param name="col">色</param>
    public static void DrawGLWireRectXZ(Vector3 center, float width, float height, Color? col)
    {
        GL.Begin(GL.LINE_STRIP);

        if (col.HasValue)
            GL.Color(col.Value);
        else
            GL.Color(Color.yellow);

        GL.Vertex3(center.x - width * 0.5f, center.y, center.z - height * 0.5f);
        GL.Vertex3(center.x - width * 0.5f, center.y, center.z + height * 0.5f);
        GL.Vertex3(center.x + width * 0.5f, center.y, center.z + height * 0.5f);
        GL.Vertex3(center.x + width * 0.5f, center.y, center.z - height * 0.5f);
        GL.Vertex3(center.x - width * 0.5f, center.y, center.z - height * 0.5f);


        GL.End();
    }
}
