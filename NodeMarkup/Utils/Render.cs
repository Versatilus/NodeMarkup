﻿using NodeMarkup.Manager;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Assertions.Must;

namespace NodeMarkup.Utils
{
    public static class Render
    {
        public static int ID_DecalSize { get; } = Shader.PropertyToID("_DecalSize");
        static int[] VerticesIdxs { get; } = new int[]
{
            1,3,2,0,// Bottom
            5,7,4,6,// Top
            1,4,0,5,// Front
            0,7,3,4,// Left
            3,6,2,7,// Back
            2,5,1,6, // Right
};
        static int[] TrianglesIdxs { get; } = new int[]
        {
                0,1,2,      1,0,3,      // Bottom
                4,5,6,      5,4,7,      // Top
                8,9,10,     9,8,11,     // Front
                12,13,14,   13,12,15,   // Left 
                16,17,18,   17,16,19,   // Back
                20,21,22,   21,20,23,    // Right
        };
        static int VCount { get; } = 24;
        static int TCount { get; } = 36;

        public static Mesh CreateMesh(int count, Vector3 size)
        {
            var vertices = new Vector3[VCount * count];
            var triangles = new int[TCount * count];
            var colors32 = new Color32[VCount * count];
            var uv = new Vector2[VCount * count];

            CreateTemp(size.x, size.y, size.z, out Vector3[] tempV, out int[] tempT);

            for (var i = 0; i < count; i += 1)
            {
                var tempColor = VerticesColor(i);

                for (var j = 0; j < VCount; j += 1)
                {
                    vertices[i * VCount + j] = tempV[j];
                    colors32[i * VCount + j] = tempColor;
                    uv[i * VCount + j] = Vector2.zero;
                }
                for (var j = 0; j < TCount; j += 1)
                {
                    triangles[i * TCount + j] = tempT[j] + (VCount * i);
                }
            }

            Bounds bounds = default;
            bounds.SetMinMax(new Vector3(-100000f, -100000f, -100000f), new Vector3(100000f, 100000f, 100000f));

            var mesh = new Mesh();
            mesh.Clear();

            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.colors32 = colors32;
            mesh.bounds = bounds;
            mesh.uv = uv;

            return mesh;
        }


        private static void CreateTemp(float length, float height, float width, out Vector3[] vertices, out int[] triangles)
        {
            Vector3[] c = new Vector3[8];

            c[0] = new Vector3(-length * .5f, -width * .5f, height * .5f); //0 --+
            c[1] = new Vector3(length * .5f, -width * .5f, height * .5f);  //1 +-+
            c[2] = new Vector3(length * .5f, -width * .5f, -height * .5f); //2 +--
            c[3] = new Vector3(-length * .5f, -width * .5f, -height * .5f);//3 ---

            c[4] = new Vector3(-length * .5f, width * .5f, height * .5f);  //4 -++
            c[5] = new Vector3(length * .5f, width * .5f, height * .5f);   //5 +++
            c[6] = new Vector3(length * .5f, width * .5f, -height * .5f);  //6 ++-
            c[7] = new Vector3(-length * .5f, width * .5f, -height * .5f); //7 -+-

            vertices = new Vector3[24];
            triangles = new int[36];

            for (var j = 0; j < 24; j += 1)
            {
                vertices[j] = c[VerticesIdxs[j]];
            }
            for (var j = 0; j < 36; j += 1)
            {
                triangles[j] = TrianglesIdxs[j];
            }
        }
        public static Color32 VerticesColor(int i) => new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, (byte)(16 * i));

        public static Material CreateMaterial()
        {
            var texture = CreateDotTexture(Color.white/*new Color32(127,127,127,255)*/);
            var material = new Material(Shader.Find("Custom/Props/Decal/Blend"))
            {
                mainTexture = texture,
                name = "NodeMarkup",
                color = new Color(1f, 1f, 1f, 1f),
                doubleSidedGI = false,
                enableInstancing = false,
                globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack,
            };
            material.EnableKeyword("MULTI_INSTANCE");

            var size = new Vector2(1.5f, 0.15f * 8);
            var tile = new Vector3(1.0f, 1.0f);
            var slopeTolerance = 3f;

            var scale = new Vector4(size.x, slopeTolerance, size.y, 0);
            var tiling = new Vector4(tile.x, 0, tile.y, 0);

            
            material.SetVector("_DecalSize", scale);
            material.SetVector("_DecalTiling", tiling);

            return material;
        }
        public static Texture2D CreateChessBoardTexture()
        {
            var height = 256;
            var width = 256;
            var texture = new Texture2D(height, width)
            {
                name = "Markup",
            };
            for (var i = 0; i < height * width; i += 1)
            {
                var row = i / height;
                var column = i % width;

                var colorRow = row / (height / 4);
                var colorColumn = column / (width / 4);
                var color = (colorColumn + colorRow) % 2 == 0 ? 0f : 1f;
                texture.SetPixel(row, column, new Color(color, color, color, 1));
            }

            texture.Apply();
            return texture;
        }
        public static Texture2D CreateDotTexture(Color color)
        {
            var height = 1;
            var width = 1;
            var texture = new Texture2D(height, width)
            {
                name = "Markup",
            };
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }
    }

    public class RenderBatch
    {
        public Vector4[] Locations { get; }
        public Vector4[] Indices { get; }
        public Vector4[] Colors { get; }
        public Mesh Mesh { get; }

        public Vector4 Size { get; }

        public RenderBatch(MarkupDash[] dashes, int count, float length)
        {
            Locations = new Vector4[count];
            Indices = new Vector4[count];
            Colors = new Vector4[count];
            Size = new Vector4(length, 3f, 0.15f);

            for (var i = 0; i < count; i += 1)
            {
                var dash = dashes[i];
                Locations[i] = dash.Position;
                Locations[i].w = dash.Angle;
                Indices[i] = new Vector4(0f, 0f, 0f, 1f);
                Colors[i] = dash.Color.ToX3Vector();
            }

            Mesh = Render.CreateMesh(count, Size);
        }

        public static IEnumerable<RenderBatch> FromDashes(MarkupDash[] dashes)
        {
            var groups = dashes.GroupBy(d => Round(d.Length));

            foreach (var group in groups)
            {
                var length = group.Key;
                var groupEnumerator = group.GetEnumerator();

                var buffer = new MarkupDash[16];
                var count = 0;

                bool isEnd = groupEnumerator.MoveNext();
                do
                {
                    buffer[count] = groupEnumerator.Current;
                    count += 1;
                    isEnd = !groupEnumerator.MoveNext();
                    if (isEnd || count == 16)
                    {
                        var batch = new RenderBatch(buffer, count, length);
                        yield return batch;
                        count = 0;
                    }
                }
                while (!isEnd);
            }

            float Round(float value)
            {
                var temp = (int)(value * 100);
                var mod = temp % 10;
                return (mod == 0 ? temp : temp - mod + 10) / 100f;
            }
        }
    }
}