﻿using System.IO;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using SlimDX;
using SlimDX.Direct3D11;

namespace SlimDXTest
{

    public class Converter
    {
        static public string[] InvalidCheck(StreamReader s, int checkNum, char split)
        {
            string[] str = SkipSpace(s).Split(split);
            if (str.Length < checkNum)
            {
                str = s.ReadLine().Split(split);
                if (str.Length < checkNum) Console.WriteLine("Invalid Stream?");
            }
            return str;
        }

        static public string[] InvalidCheck(StreamReader s, int checkNum)
        {
            return InvalidCheck(s, checkNum, ';');
        }

        static public string SkipSpace(StreamReader s)
        {
            string line;
            do
            {
                line = s.ReadLine();
            }
            while (Regex.IsMatch(line, @"^\s*$"));
            return line;
        }

        static public Vector3 ToVector3(StreamReader s)
        {
            Vector3 v = new Vector3();
            string[] str = InvalidCheck(s, 3);
            v.X = Convert.ToSingle(str[0]);
            v.Y = Convert.ToSingle(str[1]);
            v.Z = Convert.ToSingle(str[2]);
            return v;
        }

        static public Vector2 ToVector2(StreamReader s)
        {
            Vector2 v = new Vector2();
            string line = SkipSpace(s);
            try
            {
                //なぜか区切り文字が異なる場合があるので違うパターンも用意する
                string[] str = line.Split(';');
                v.X = Convert.ToSingle(str[0]);
                str = str[1].Split(';');
                v.Y = Convert.ToSingle(str[0]);
                return v;
            }
            catch
            {
                string[] str = line.Split(',');
                v.X = Convert.ToSingle(str[0]);
                str = str[1].Split(';');
                v.Y = Convert.ToSingle(str[0]);
                return v;
            }
        }

        static public int ToInt(StreamReader s)
        {
            return ToInt(s, ';');
        }

        static public int ToInt(StreamReader s, char split)
        {
            string[] str = InvalidCheck(s, 1, split);
            int num = 0;
            try
            {
                num = Convert.ToInt32(str[0]);
            }
            catch
            {
                str = str[0].Split(';');
                num = Convert.ToInt32(str[0]);
            }
            return num;
        }

        static public float ToFloat(StreamReader s)
        {
            string[] str = InvalidCheck(s, 1);
            return Convert.ToSingle(str[0]);
        }

        static public Color4 ToColor(StreamReader s)
        {
            string[] str = InvalidCheck(s, 4);
            Color4 c = new Color4();
            c.Red = Convert.ToSingle(str[0]);
            c.Green = Convert.ToSingle(str[1]);
            c.Blue = Convert.ToSingle(str[2]);
            c.Alpha = Convert.ToSingle(str[3]);
            return c;
        }

        static public Color4 ToColor(StreamReader s, float alpha)
        {
            Color4 c = new Color4();
            string[] str = InvalidCheck(s, 3);
            c.Red = Convert.ToSingle(str[0]);
            c.Green = Convert.ToSingle(str[1]);
            c.Blue = Convert.ToSingle(str[2]);
            c.Alpha = alpha;
            return c;
        }
    }

    /// <summary>
    /// 頂点リスト 
    /// </summary>
    public class VertexList
    {
        private int vertexCount;
        public int VertexCount
        {
            get { return vertexCount; }
        }

        public Vector3[] vertex;

        public VertexList(StreamReader s)
        {
            vertexCount = Converter.ToInt(s);
            Console.WriteLine("VertexList.Length: " + vertexCount);

            vertex = new Vector3[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                vertex[i] = Converter.ToVector3(s);
        }
    }

    /// <summary>
    /// メッシュリスト 
    /// ポリゴンを構成する頂点インデックスの集合 
    /// </summary>
    public class MeshList
    {
        private int meshCount;
        public int MeshCount
        {
            get { return meshCount; }
        }

        public List<int[]> mesh;

        public MeshList(StreamReader s, VertexList l)
        {
            meshCount = Converter.ToInt(s);
            Console.WriteLine("MeshList.Length: " + meshCount);

            // 頂点インデックスの保存 
            mesh = new List<int[]>();
            for (int i = 0; i < meshCount; i++)
            {
                // インデックスの抽出 
                string[] str = s.ReadLine().Split(';');
                mesh.Add(new int[Convert.ToInt32(str[0])]);

                // 数値の抽出
                str = str[1].Split(',');
                for (int j = 0; j < mesh[i].Length; j++)
                {
                    try
                    {
                        mesh[i][j] = Convert.ToInt32(str[j]);
                    }
                    catch
                    {
                        Console.WriteLine("Invalid MeshList: " + i);
                    }
                }
            }
        }
    }

    /// <summary>
    /// 頂点マテリアルリスト 
    /// マテリアルを利用する頂点インデックスの集合 
    /// </summary>
    public class MeshMaterialList
    {
        int materialCount;
        public int MaterialCount
        {
            get { return materialCount; }
        }

        int polygonCount;
        public int PolygonCount
        {
            get { return polygonCount; }
        }

        public int[] materialIndex;
        public string[] materialReferens;

        public MeshMaterialList(StreamReader s)
        {
            materialCount = Converter.ToInt(s);
            Console.WriteLine("MeshMaterialList.Length: " + materialCount);
            polygonCount = Converter.ToInt(s);

            materialIndex = new int[polygonCount];
            for (int i = 0; i < polygonCount; i++)
            {
                materialIndex[i] = Converter.ToInt(s, ',');
            }

            materialReferens = new string[materialCount];
            for (int i = 0; i < materialCount; i++)
            {
                string[] str = s.ReadLine().Split('{');
                str = str[1].Split('}');
                materialReferens[i] = str[0];
            }

            string buf = s.ReadLine();
            if (!Regex.IsMatch(buf, "}"))
                throw new Exception("MeshMaterialList End?: " + buf);
        }
    }

    /// <summary>
    /// 法線リスト 
    /// </summary>
    public class MeshNormalList
    {
        int normalCount;
        public int NormalCount
        {
            get { return normalCount; }
        }

        public int NormalIndexCount
        {
            get { return normalIndex.Count; }
        }

        public Vector3[] normals;   // 法線データ 
        public List<int[]> normalIndex; // どの頂点がどの法線データを参照しているか 

        public MeshNormalList(StreamReader s)
        {
            normalCount = Converter.ToInt(s);
            Console.WriteLine("MeshNormalList.Length: " + normalCount);

            // 法線の読み込み 
            normals = new Vector3[normalCount];
            for (int i = 0; i < normalCount; i++)
            {
                normals[i] = Converter.ToVector3(s);
            }

            // インデックスの読み込み 
            normalIndex = new List<int[]>();
            string read = s.ReadLine();
            read = s.ReadLine();    // カウントの読み捨て 
            while (!Regex.IsMatch(read, "}"))
            {
                string[] str = read.Split(';');
                int[] buf = new int[Convert.ToInt32(str[0])];
                str = str[1].Split(',');
                for (int i = 0; i < buf.Length; i++)
                {
                    buf[i] = Convert.ToInt32(str[i]);
                }
                normalIndex.Add(buf);
                read = s.ReadLine();
            }
            Console.WriteLine("MeshNormalIndex.Length: " + normalIndex.Count);
        }
    }

    /// <summary>
    /// UVリスト 
    /// </summary>
    public class MeshTextureCoordList
    {
        int uvCount;
        public int UVCount
        {
            get { return uvCount; }
        }

        public Vector2[] uvs;

        public MeshTextureCoordList(StreamReader s)
        {
            uvCount = Converter.ToInt(s);
            Console.WriteLine("MeshTextureCoordList.Length: " + uvCount);

            uvs = new Vector2[uvCount];
            for (int i = 0; i < uvCount; i++)
            {
                uvs[i] = Converter.ToVector2(s);
            }
            s.ReadLine();
        }
    }

    public class MeshSection
    {
        private string name;
        public string Name
        {
            get { return name; }
        }

        public MeshList meshList;
        public VertexList vtxList;
        public MeshMaterialList matList;
        public MeshNormalList normList;
        public MeshTextureCoordList uvList;

        public MeshSection(StreamReader sr, string name)
        {
            this.name = name;
            vtxList = new VertexList(sr);
            meshList = new MeshList(sr, vtxList);

            // 他の何かがある場合 
            // 閉じるまで繰り返し 
            string read = sr.ReadLine();
            while (!Regex.IsMatch(read, "}"))
            {
                if (Regex.IsMatch(read, "MeshMaterialList"))
                {
                    matList = new MeshMaterialList(sr);
                }
                else if (Regex.IsMatch(read, "MeshNormals"))
                {
                    normList = new MeshNormalList(sr);
                }
                else if (Regex.IsMatch(read, "MeshTextureCoords"))
                {
                    uvList = new MeshTextureCoordList(sr);
                }
                read = sr.ReadLine();
            }
        }
    }

    public class MaterialList
    {
        public int MaterialCount
        {
            get { return materials.Count; }
        }

        public List<Material> materials;

        public MaterialList(StreamReader s)
        {
            materials = new List<Material>();
        }

        public void AddMaterial(StreamReader s, string name)
        {
            materials.Add(new Material(s, name));
        }
    }

    /// <summary>
    /// マテリアル 
    /// </summary>
    public class Material
    {
        private string name;
        public string Name
        {
            get { return name; }
        }

        private Color4 diffuseColor;
        public Color4 DiffuseColor
        {
            get { return diffuseColor; }
        }

        private float specularity;
        public float Specularity
        {
            get { return specularity; }
        }

        private Color4 specularColor;
        public Color4 SpecularColor
        {
            get { return specularColor; }
        }

        private Color4 emissionColor;
        public Color4 EmissionColor
        {
            get { return emissionColor; }
        }

        private string textureFileName;
        public string TextureFileName
        {
            get { return textureFileName; }
        }

        public Material(StreamReader s, string name)
        {
            this.name = name;
            diffuseColor = Converter.ToColor(s);
            specularity = Converter.ToFloat(s);
            specularColor = Converter.ToColor(s, 1.0f);
            emissionColor = Converter.ToColor(s, 1.0f);
            string str = s.ReadLine();
            if (Regex.IsMatch(str, "TextureFilename"))
            {
                str = s.ReadLine();
                string[] buf = str.Split('"');
                textureFileName = buf[1];
            }
            else
            {
                textureFileName = "";
            }
        }
    }
}
