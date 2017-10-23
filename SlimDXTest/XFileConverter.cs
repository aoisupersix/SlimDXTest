using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SlimDXTest
{
    public class XFileConverter
    {
        public string FilePath { get; }
        string folderPath;
        string fileName;
        string localPath;
        StreamReader sr;

        public MeshSection meshSection;
        public MaterialList matList;

        // フォルダのパス取得 
        // 最後にスラッシュアリ
        private string GetFolderPath()
        {
            string[] str = FilePath.Split('/');
            string result = "";
            for (int i = 0; i < str.Length - 1; i++)
                result += str[i] + "/";
            return result;
        }

        private string GetFileName()
        {
            string[] str = FilePath.Split('/');
            return str[str.Length - 1];
        }

        // 1行1行解釈してクラスを作ってく 
        private void Parser(string s)
        {
            Console.WriteLine("Parse:" + s);
            if (Regex.IsMatch(s, @"^\s*Material "))
            {
                s = Regex.Replace(s, "Material ", "");  // マッチさせる前に邪魔なのは消す 
                Match m = Regex.Match(s, "\\w+");
                Console.WriteLine("Converter: Material.Add:" + m.Value);
                matList.AddMaterial(sr, m.Value);
            }
            else if (Regex.IsMatch(s, @"^\s*Mesh "))
            {
                s = Regex.Replace(s, "Mesh ", "");
                Match m = Regex.Match(s, "\\w+");
                Console.WriteLine(m.Value);
                meshSection = new MeshSection(sr, m.Value);
            }
        }

        private void CreateFolder()
        {
            string buf = folderPath;
            buf.Replace("/", "\\");
            System.IO.Directory.CreateDirectory(buf + "Materials"); // とりあえず、マテリアルのフォルダ作成 
        }

        public XFileConverter(string fullPath)
        {

            fullPath = MaterialFix(fullPath);

            FilePath = fullPath;
            folderPath = System.IO.Path.GetDirectoryName(fullPath) + @"\";
            fileName = System.IO.Path.GetFileName(fullPath);

            Console.WriteLine("FilePath:" + FilePath);
            Console.WriteLine("folderPath:" + folderPath);
            Console.WriteLine("fileName:" + fileName);

            CreateFolder();
            sr = new StreamReader(FilePath);
            matList = new MaterialList(sr);

            while (!sr.EndOfStream)
            {
                Parser(sr.ReadLine());
            }
        }

        /// <summary>
        /// マテリアルを直接書いている場合、参照呼び出しに変換する。
        /// </summary>
        /// <param name="fullPath">元のファイルパス</param>
        /// <returns>変換後のファイルパス</returns>
        private string MaterialFix(string fullPath)
        {
            Console.WriteLine("Convert:MaterialFix:)");

            string name = System.IO.Path.GetFileName(fullPath).Split('.')[0];
            string newPath = System.IO.Path.GetDirectoryName(fullPath) + @"\" + name + "_cnv.x";

            if (System.IO.File.Exists(newPath))
            {
                //変換したファイルがすでにあるので飛ばす
                Console.WriteLine("ConvertedXFile is already Exists.");
                return newPath;
            }

            StreamReader streamReader = new StreamReader(fullPath);

            //参照呼び出しか判定
            List<string> xfile = new List<string>();
            bool isHeaderBlock = false, isMeshBlock = false;
            int headerPos = 1;
            int matName = 0;
            while (!streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                xfile.Add(line);
                if (Regex.IsMatch(line, @"^\s*Header\s*\{"))
                {
                    //ヘッダーブロック内
                    isHeaderBlock = true;
                    continue;
                }

                if (isHeaderBlock)
                {
                    //ヘッダーブロック終了判定
                    if (Regex.IsMatch(line, @"\}"))
                    {
                        isHeaderBlock = false;
                        headerPos = xfile.Count;
                        continue;
                    }
                }
                else
                {
                    if (Regex.IsMatch(line, @"MeshMaterialList\s*\{"))
                    {
                        //マテリアルリストブロック内
                        isMeshBlock = true;
                        continue;
                    }
                    if (isMeshBlock)
                    {
                        if (Regex.IsMatch(line, @"\{.+\}"))
                        {
                            //参照呼び出しである
                            Console.WriteLine("マテリアル参照呼び出し");
                            return fullPath;
                        }
                        else if (Regex.IsMatch(line, @"Material\s*\{"))
                        {
                            Console.WriteLine("----マテリアル直接呼び出し----");
                            //直接呼び出しなので移動する
                            List<string> material = new List<string>();
                            material.Add("Material M" + matName + " {");
                            xfile[xfile.Count - 1] = "  { M" + matName + " }";
                            matName++;
                            int block = 1;
                            while (block > 0)
                            {
                                line = streamReader.ReadLine();
                                Console.WriteLine("line:" + line);
                                if (Regex.IsMatch(line, @"\}"))
                                    block--;
                                material.Add(CreateSpace(block) + line.Trim());
                                if (Regex.IsMatch(line, @"\{"))
                                    block++;
                            }
                            xfile.InsertRange(headerPos, material);
                        }
                        else if (Regex.IsMatch(line, @"\}"))
                            isMeshBlock = false;
                    }
                }
            }

            //書き出し
            System.IO.File.WriteAllLines(newPath, xfile.ToArray());
            return newPath;
        }

        private string CreateSpace(int num)
        {
            string result = "";
            for (int i = 0; i < num; i++)
            {
                result = result + " ";
            }
            return result;
        }

   //     private void EntryVerticesForMesh(Mesh mesh)
   //     {
   //         if (meshSection.vtxList.vertex.Length > 65000)
   //             throw new Exception("A mesh may not have more than 65000 vertices.");
   //         mesh.vertices = meshSection.vtxList.vertex;
   //     }

   //     private void EntryUVForMesh(Mesh mesh)
   //     {
   //         mesh.uv = meshSection.uvList.uvs;
   //     }

   //     // サブメッシュの登録 
   //     private void EntrySubMeshForMesh(Mesh mesh)
   //     {
   //         MeshList meshList = meshSection.meshList;
   //         MeshMaterialList matList = meshSection.matList;
   //         mesh.subMeshCount = matList.MaterialCount;  // サブメッシュの数をここで設定

   //         /*
			//for (int i = 0; i < meshList.MeshCount; i++) {
			//	mesh.SetTriangles(meshList.mesh[i], matList.materialIndex[i]);
			//}*/

   //         for (int i = 0; i < matList.MaterialCount; i++)
   //         {
   //             List<int> submesh = new List<int>();
   //             for (int j = 0; j < meshList.MeshCount; j++)
   //             {
   //                 if (i == matList.materialIndex[j])
   //                 {
   //                     foreach (int num in meshList.mesh[j])
   //                         submesh.Add(num);
   //                 }
   //             }
   //             int[] buf = new int[submesh.Count];
   //             submesh.CopyTo(buf);
   //             mesh.SetTriangles(buf, i);
   //         }
   //     }

   //     public UnityEngine.Object CreatePrefab()
   //     {
   //         string path = folderPath + fileName.Split('.')[0] + ".prefab";
   //         return PrefabUtility.CreateEmptyPrefab(path);
   //     }

   //     private void EntryNormal(Mesh mesh)
   //     {
   //         if (meshSection.normList.NormalCount != 0)
   //             mesh.normals = meshSection.normList.normals;
   //     }

   //     // メッシュの生成 
   //     public Mesh CreateMesh()
   //     {
   //         Mesh mesh = new Mesh();
   //         EntryVerticesForMesh(mesh);
   //         EntryUVForMesh(mesh);
   //         EntrySubMeshForMesh(mesh);
   //         EntryNormal(mesh);
   //         AssetDatabase.CreateAsset(mesh, folderPath + fileName.Split('.')[0] + ".asset");
   //         return mesh;
   //     }

   //     // マテリアルの登録 
   //     private UnityEngine.Material EntryMaterial(int i)
   //     {
   //         UnityEngine.Material mat = new UnityEngine.Material(Shader.Find("VertexLit"));
   //         Material source = matList.materials[i];
   //         Texture tex = null;

   //         // テクスチャを貼る 
   //         if (source.TextureFileName != "")
   //         {
   //             tex = AssetDatabase.LoadAssetAtPath(folderPath + source.TextureFileName, typeof(Texture)) as Texture;
   //             mat.mainTexture = tex;
   //             mat.SetTextureScale("_MainTex", new Vector2(1, -1));
   //         }

   //         mat.color = source.DiffuseColor;
   //         mat.SetColor("_SpecColor", source.SpecularColor);
   //         mat.SetColor("_Emission", source.EmissionColor);
   //         mat.SetFloat("_Shiness", source.Specularity);
   //         mat.name = this.fileName + "_" + source.Name;

   //         AssetDatabase.CreateAsset(mat, folderPath + "Materials\\" + mat.name + ".asset");
   //         return mat;
   //     }

   //     public UnityEngine.Material[] CreateMaterials()
   //     {
   //         UnityEngine.Material[] material = new UnityEngine.Material[matList.MaterialCount];
   //         for (int i = 0; i < matList.MaterialCount; i++)
   //         {
   //             material[i] = EntryMaterial(i);
   //         }
   //         return material;
   //     }

   //     public void ReplacePrefab(UnityEngine.Object prefab, Mesh mesh, UnityEngine.Material[] materials)
   //     {
   //         GameObject obj = new GameObject(fileName.Split('.')[0]);
   //         MeshFilter filter = obj.AddComponent<MeshFilter>();
   //         filter.mesh = mesh;
   //         MeshRenderer mren = obj.AddComponent<MeshRenderer>();
   //         mren.sharedMaterials = materials;
   //         PrefabUtility.ReplacePrefab(obj, prefab);
   //     }

   //     public void MeshTest()
   //     {
   //         Mesh mesh = new Mesh();
   //         EntryVerticesForMesh(mesh);
   //         EntryUVForMesh(mesh);
   //         EntrySubMeshForMesh(mesh);
   //         EntryNormal(mesh);
   //     }
    }
}
