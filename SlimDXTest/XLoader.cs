using System;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using SlimDX;

namespace SlimDXTest
{
    class XLoader
    {
        public string FilePath { get; }
        private string[] Line;
        private Vector3[] Meshes;

        XLoader(string filePath)
        {
            FilePath = filePath;
            LoadFile(filePath);
        }
        public void LoadFile(string filePath)
        {
            //ファイルのロード
            try
            {
                Line = System.IO.File.ReadAllLines(filePath);
            }
            catch (Exception e)
            {
                System.Console.WriteLine(e.Message);
                throw;
            }
        }

        private Vector3[] GetMeshes()
        {
            if (Meshes == null)
            {
                //メッシュの読み込み定義位置の取得
                int meshIndex;
                if ((meshIndex = GetMeshIndex(Line)) == -1)
                    throw new FormatException();
                int meshNum = (int)GetDigit(Line[meshIndex])[0];

                //メッシュの読み込み
                Meshes = new Vector3[meshNum];
                meshIndex++;
                for (int i = 0; i < Meshes.Length; i++)
                {
                    Console.WriteLine("mesh:" + Line[i + meshIndex]);
                    List<float> coord = GetDigit(Line[i + meshIndex]);
                    float x = coord[0], y = coord[1], z = coord[2];
                    Meshes[i] = new Vector3(x, y, -z);
                    Console.WriteLine("i[" + i + "]=x:" + Meshes[i].X + ",y:" + Meshes[i].Y + ",z:" + Meshes[i].Z);
                }
            }
            return Meshes;
        }

        /// <summary>
        /// メッシュ定義の開始行数を取得する
        /// </summary>
        /// <param name="lines">xファイル</param>
        /// <returns>メッシュ定義の開始行数</returns>
        private int GetMeshIndex(string[] lines)
        {
            //メッシュ定義の切り出し
            int meshIndex = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                Match match = Regex.Match(lines[i], @"^\s*Mesh\s*\{\s*");
                Console.WriteLine("line:" + i + ",reg:" + match.Value);
                if (match.Success)
                {
                    //メッシュ定義の先頭を発見
                    meshIndex = i + 1;
                    break;
                }
            }
            return meshIndex;
        }

        /// <summary>
        /// 引数に与えられた文字列の数値を切り出す
        /// </summary>
        /// <param name="line">切り出し対象の文字列</param>
        /// <returns>数値ごとのList</returns>
        private List<float> GetDigit(string line)
        {
            List<float> digits = new List<float>();
            foreach(Match m in Regex.Matches(line, @"\d+(?:\.\d+)?"))
            {
                digits.Add(float.Parse(m.Value));
            }
            return digits;
        }
    }
}