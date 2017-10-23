using System;
using System.Collections.Generic;
using SlimDX;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using D3DComp = SlimDX.D3DCompiler;

namespace SlimDXTest
{
    /// <summary>
    /// Xファイルからのモデルをまとめて管理する
    /// </summary>
    class Model
    {
        const int VTXNUM_SQUARE = 4; //頂点の数が4つの頂点インデックス

        public string FilePath { get; set; }
        public string OldPath { get; }
        XFileConverter Cnv;

        Dx11.InputLayout vertexLayout;
        Dx11.Buffer vertexBuffer;
        Dx11.Buffer indexBuffer;
        List<VertexPositionTexture> vtxList = new List<VertexPositionTexture>();
        List<uint> indices = new List<uint>();
        TextureManager textureManager = new TextureManager();

        public Model(string filePath)
        {
            this.OldPath = filePath;
            Cnv = new XFileConverter(this.OldPath, CreateNewPath(filePath));
            this.FilePath = Cnv.Import();
        }

        /// <summary>
        /// 変換後のXファイルのパスを生成する。
        /// とりあえず暫定として1階層上のディレクトリをtmpフォルダに入れる
        /// </summary>
        /// <param name="filePath">xファイルのパス</param>
        /// <returns>変換後のファイルのパス</returns>
        private string CreateNewPath(string filePath)
        {
            string currentDirectoryPath = System.IO.Path.GetFullPath("./");
            string tmpPath = currentDirectoryPath + @"temp\";

            string[] splitPath = System.IO.Path.GetFullPath(filePath).Split('\\');
            string origin = splitPath[splitPath.Length - 2] + @"\" + splitPath[splitPath.Length - 1];

            return tmpPath + origin;
        }

        /// <summary>
        /// マテリアル名から一致するマテリアルを調べる
        /// </summary>
        /// <param name="materialReference">参照マテリアル名</param>
        /// <returns>参照先マテリアル</returns>
        private Material GetMaterialFromReference(string materialReference)
        {
            foreach (Material m in Cnv.matList.materials)
            {
                if (m.Name.Equals(materialReference))
                    return m;
            }
            return null;
        }

        /// <summary>
        /// このモデルのバッファをデバイスにセットする
        /// </summary>
        public void InitModelInputAssembler(Dx11.Device device)        {

            device.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
            device.ImmediateContext.InputAssembler.SetVertexBuffers(
                0,
                new Dx11.VertexBufferBinding(vertexBuffer, VertexPositionTexture.SizeInBytes, 0)
                );

            device.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Dxgi.Format.R32_UInt, 0);

            device.ImmediateContext.InputAssembler.PrimitiveTopology
                = Dx11.PrimitiveTopology.TriangleList;
        }

        /// <summary>
        /// モデルを描画する
        /// </summary>
        public void DrawModel(Dx11.Device device,Dx11.Effect effect)
        {
            int startIdx = 0, indexCount = 0;
            for (int i = 0; i < Cnv.meshSection.matList.PolygonCount; i++)
            {
                //マテリアル名から一致するマテリアルを調べる
                int matNum = Cnv.meshSection.matList.materialIndex[i];
                string refName = Cnv.meshSection.matList.materialReferens[matNum];

                Material m = GetMaterialFromReference(refName);
                //Console.WriteLine("mat:" + matNum + "-> ref:" + refName + ",matName:" + m.Name);

                //テクスチャを利用しているかどうか
                if (!m.TextureFileName.Equals(""))
                {
                    effect.GetVariableByName("tex").AsScalar().Set(true);
                    effect.GetVariableByName("normalTexture").AsResource().SetResource(textureManager.GetTexture(m.Name));
                }
                else
                {
                    effect.GetVariableByName("tex").AsScalar().Set(false);
                    effect.GetVariableByName("mat").AsScalar().Set(true);
                    effect.GetVariableByName("matColor").AsVector().Set(m.DiffuseColor);
                }

                //頂点数が4の場合は2つに分割する
                if (Cnv.meshSection.meshList.mesh[i].Length == VTXNUM_SQUARE)
                {
                    indexCount = 6;
                    effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
                    device.ImmediateContext.DrawIndexed(indexCount, startIdx, 0);
                }
                else
                {
                    indexCount = 3;
                    effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
                    device.ImmediateContext.DrawIndexed(indexCount, startIdx, 0);
                }
                startIdx += indexCount;
            }

            effect.GetVariableByName("tex").AsScalar().Set(false);
            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(device.ImmediateContext);
            device.ImmediateContext.DrawIndexed(indices.Count, 0, 0);
        }

        /// <summary>
        /// 初期化処理を行う
        /// </summary>
        public void Init(Dx11.Device device, Dx11.Effect effect)
        {
            InitVertexLayout(device, effect);
            InitVertexBuffer(device);
            InitIndexBuffer(device);
            InitTexture(device);
        }

        private void InitVertexLayout(Dx11.Device device, Dx11.Effect effect)
        {
            vertexLayout = new Dx11.InputLayout(
                device,
                effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature, VertexPositionTexture.VertexElements);
        }

        /// <summary>
        /// 頂点バッファの格納
        /// </summary>
        /// <param name="device">GraphicsDevice</param>
        private void InitVertexBuffer(Dx11.Device device)
        {
            for (int i = 0; i < Cnv.meshSection.vtxList.VertexCount; i++)
            {
                Vector3 vtxPos = Cnv.meshSection.vtxList.vertex[i];
                Vector2 uvPos = Cnv.meshSection.uvList.uvs[i];

                Console.WriteLine("Model:" + OldPath + " -> InitVertexBuffer");
                Console.WriteLine("AddVertex [" + i + "] : x:" + vtxPos.X + ",y:" + vtxPos.Y + ",z:" + vtxPos.Z);
                Console.WriteLine("AddUv [" + i + "] : x:" + uvPos.X + ",y:" + uvPos.Y);

                vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(vtxPos, uvPos));
            }

            vertexBuffer = MyDirectXHelper.CreateVertexBuffer(device, vtxList.ToArray());
        }

        /// <summary>
        /// インデックスバッファの格納
        /// </summary>
        /// <param name="device">GraphicsDevice</param>
        private void InitIndexBuffer(Dx11.Device device)
        {
            int counter = 0;
            foreach (int[] meshes in Cnv.meshSection.meshList.mesh)
            {
                Console.WriteLine("index:" + counter);
                if (meshes.Length == VTXNUM_SQUARE)
                {
                    Console.WriteLine("SQUARE:1 " + meshes[0] + ", " + meshes[1] + ", " + meshes[3]);
                    Console.WriteLine("SQUARE:2 " + meshes[1] + ", " + meshes[2] + ", " + meshes[3]);

                    //四角形を2つに分割してインデックスバッファに格納
                    indices.Add((uint)meshes[0]);
                    indices.Add((uint)meshes[1]);
                    indices.Add((uint)meshes[3]);

                    indices.Add((uint)meshes[1]);
                    indices.Add((uint)meshes[2]);
                    indices.Add((uint)meshes[3]);
                }
                else
                {
                    Console.WriteLine("TRIANGLE:1 " + meshes[0] + ", " + meshes[1] + ", " + meshes[2]);
                    //三角形なのでそのままインデックスバッファに格納
                    indices.Add((uint)meshes[0]);
                    indices.Add((uint)meshes[1]);
                    indices.Add((uint)meshes[2]);
                }
                counter++;
            }
            indexBuffer = MyDirectXHelper.CreateIndexBuffer(device, indices.ToArray());
        }

        /// <summary>
        /// テクスチャの格納
        /// </summary>
        /// <param name="device">GraphicsDevice</param>
        private void InitTexture(Dx11.Device device)
        {
            string directoryPath = System.IO.Path.GetDirectoryName(this.FilePath) + "/";

            //テクスチャの抽出
            for (int i = 0; i < Cnv.matList.MaterialCount; i++)
            {
                if (!Cnv.matList.materials[i].TextureFileName.Equals(""))
                {
                    //テクスチャ登録
                    try
                    {
                        textureManager.SetTexture(
                            Cnv.matList.materials[i].Name,
                            Dx11.ShaderResourceView.FromFile(
                                device, directoryPath + Cnv.matList.materials[i].TextureFileName,
                                new Dx11.ImageLoadInformation()
                                {
                                    Format = Dxgi.Format.R32G32B32A32_Float,
                                    FilterFlags = Dx11.FilterFlags.Triangle,
                                    MipFilterFlags = Dx11.FilterFlags.Triangle,
                                    Usage = Dx11.ResourceUsage.Default,
                                    BindFlags = Dx11.BindFlags.ShaderResource,
                                    MipLevels = -1
                                }
                                )
                            );
                    }
                    catch
                    {
                        Console.WriteLine("Texture:" + Cnv.matList.materials[i].TextureFileName + " is not found.");
                        continue;
                    }
                }
            }
        }

        public void Unload()
        {
            vertexLayout.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
            textureManager.Dispose();
        }
    }

    /// <summary>
    /// テクスチャの管理クラス
    /// </summary>
    class TextureManager
    {
        private List<Dx11.ShaderResourceView> textures = new List<Dx11.ShaderResourceView>();
        Dictionary<string, int> texIndex = new Dictionary<string, int>();

        public List<Dx11.ShaderResourceView> GetAllTextures()
        {
            return textures;
        }

        public Dx11.ShaderResourceView GetTexture(string matName)
        {
            try
            {
                int index = texIndex[matName];
                return textures[index];
            }
            catch
            {
                Console.WriteLine("TextureManager: GetTexture matNum:" + matName + " is not found.");
                return null;
            }
        }


        public void SetTexture(string matName, Dx11.ShaderResourceView tex)
        {
            textures.Add(tex);
            texIndex.Add(matName, textures.Count - 1);
        }

        public void Dispose()
        {
            foreach(Dx11.ShaderResourceView texture in textures)
            {
                texture.Dispose();
            }
        }
    }

    /// <summary>
    /// 各頂点の情報
    /// </summary>
    struct VertexPositionTexture
    {
        public Vector3 Position;
        public Vector2 TextureCoordinate;

        public static readonly Dx11.InputElement[] VertexElements = new[]
        {
            new Dx11.InputElement
            {
                SemanticName = "SV_Position",
                Format = Dxgi.Format.R32G32B32_Float
            },
            new Dx11.InputElement
            {
                SemanticName = "TEXCOORD",
                Format = Dxgi.Format.R32G32_Float,
                AlignedByteOffset = Dx11.InputElement.AppendAligned
            }
        };

        public static int SizeInBytes
        {
            get { return System.Runtime.InteropServices.Marshal.SizeOf(typeof(VertexPositionTexture)); }
        }
    }
}
