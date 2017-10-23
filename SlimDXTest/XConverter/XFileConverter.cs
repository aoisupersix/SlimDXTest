using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace SlimDXTest
{
    public class XFileConverter
    {
        public string FilePath { get; }
        string fileName;
        StreamReader sr;

        public MeshSection meshSection;
        public MaterialList matList;

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

        public XFileConverter(string filePath, string newPath)
        {
            //Xファイルの前変換
            Console.WriteLine("XfileImporter Initialized.");
            this.FilePath = MaterialFix(filePath, newPath);
            fileName = System.IO.Path.GetFileName(this.FilePath);

            Console.WriteLine("FilePath:" + FilePath);
            Console.WriteLine("fileName:" + fileName);
        }

        public string Import()
        {
            sr = new StreamReader(FilePath);
            matList = new MaterialList(sr);

            while (!sr.EndOfStream)
            {
                Parser(sr.ReadLine());
            }
            return this.FilePath;
        }

        /// <summary>
        /// マテリアルを直接書いている場合、参照呼び出しに変換する。
        /// </summary>
        /// <param name="fullPath">元のファイルパス</param>
        /// <returns>変換後のファイルパス</returns>
        private string MaterialFix(string filePath, string newPath)
        {
            Console.WriteLine("Convert:MaterialFix:)");

            string name = System.IO.Path.GetFileName(filePath).Split('.')[0];

            if (System.IO.File.Exists(newPath))
            {
                //変換したファイルがすでにあるので飛ばす
                Console.WriteLine("ConvertedXFile is already Exists.");
                return newPath;
            }

            StreamReader streamReader = new StreamReader(filePath);

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
                            return filePath;
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
                                if(Regex.IsMatch(line, "\".+\"\\s*;"))
                                {
                                    //テクスチャファイルの移動
                                    string texName = line.Split('"')[1];
                                    string currentDir = System.IO.Path.GetDirectoryName(filePath) + "\\";
                                    string newDir = System.IO.Path.GetDirectoryName(newPath) + "\\";
                                    if (System.IO.File.Exists(currentDir + texName) && !System.IO.File.Exists(newDir + texName))
                                    {
                                        //ファイルが存在するので移動
                                        System.IO.File.Copy(currentDir + texName, newDir + texName);
                                        Console.WriteLine("CopyFile:" + currentDir + texName + " -> " + newDir + texName);
                                    }
                                }
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
    }
}
