using System;
using System.Collections.Generic;
using SlimDX;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using D3DComp = SlimDX.D3DCompiler;

namespace SlimDXTest
{
    class View : Core
    {
        public string filePath = "./244End.x";
        const int VTXNUM_SQUARE = 4;

        Dx11.Effect effect;
        Dx11.InputLayout vertexLayout;
        Dx11.Buffer vertexBuffer;
        Dx11.Buffer indexBuffer;

        List<VertexPositionTexture> vtxList = new List<VertexPositionTexture>();
        List<uint> indices = new List<uint>();
        TextureManagement textureMan = new TextureManagement();
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem ファイルFToolStripMenuItem;
        XFileConverter cnv;

        protected override void Draw()
        {
            //背景塗りつぶし
            GraphicsDevice.ImmediateContext.ClearRenderTargetView(
                RenderTarget,
                new SlimDX.Color4(1, 0.39f, 0.58f, 0.93f)
            );

            //深度バッファ
            GraphicsDevice.ImmediateContext.ClearDepthStencilView(
                DepthStencil,
                Dx11.DepthStencilClearFlags.Depth,
                1,
                0);

            UpdateCamera();

            InitModelInputAssembler();
            DrawModel();

            SwapChain.Present(0, Dxgi.PresentFlags.None);
        }

        private void UpdateCamera()
        {
            double time = System.Environment.TickCount / 1000d;

            Matrix world = Matrix.LookAtRH(
                new Vector3((float)System.Math.Cos(time), 0, (float)System.Math.Sin(time)),
                new Vector3(),
                new Vector3(0, 1, 0)
                );

            var view = Matrix.LookAtRH(
                new Vector3(0, 10, -10), new Vector3(0, 5, 0), new Vector3(0, 1, 0)
            );

            var projection = Matrix.PerspectiveFovRH(
                (float)Math.PI / 2, ClientSize.Width / ClientSize.Height, 0.1f, 1000
            );

            effect.GetVariableByName("World").AsMatrix().SetMatrix(world);
            effect.GetVariableByName("View").AsMatrix().SetMatrix(view);
            effect.GetVariableByName("Projection").AsMatrix().SetMatrix(projection);
        }

        private void InitModelInputAssembler()
        {

            GraphicsDevice.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
            GraphicsDevice.ImmediateContext.InputAssembler.SetVertexBuffers(
                0,
                new Dx11.VertexBufferBinding(vertexBuffer, VertexPositionTexture.SizeInBytes, 0)
                );

            GraphicsDevice.ImmediateContext.InputAssembler.SetIndexBuffer(indexBuffer, Dxgi.Format.R32_UInt, 0);

            GraphicsDevice.ImmediateContext.InputAssembler.PrimitiveTopology
                = Dx11.PrimitiveTopology.TriangleList;
        }

        private void DrawModel()
        {
            int startIdx = 0, indexCount = 0;
            for (int i = 0; i < cnv.meshSection.matList.PolygonCount; i++)
            {
                //マテリアル名から一致するマテリアルを調べる
                int matNum = cnv.meshSection.matList.materialIndex[i];
                string refName = cnv.meshSection.matList.materialReferens[matNum];

                Material m = GetMaterialFromReference(refName);
                //Console.WriteLine("mat:" + matNum + "-> ref:" + refName + ",matName:" + m.Name);

                //テクスチャを利用しているかどうか
                if (!m.TextureFileName.Equals(""))
                {
                    effect.GetVariableByName("tex").AsScalar().Set(true);
                    effect.GetVariableByName("normalTexture").AsResource().SetResource(textureMan.GetTexture(m.Name));
                }
                else
                {
                    effect.GetVariableByName("tex").AsScalar().Set(false);
                    effect.GetVariableByName("mat").AsScalar().Set(true);
                    effect.GetVariableByName("matColor").AsVector().Set(m.DiffuseColor);
                }

                //頂点数が4の場合は2つに分割する
                if (cnv.meshSection.meshList.mesh[i].Length == VTXNUM_SQUARE)
                {
                    indexCount = 6;
                    effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(GraphicsDevice.ImmediateContext);
                    GraphicsDevice.ImmediateContext.DrawIndexed(indexCount, startIdx, 0);
                }
                else
                {
                    indexCount = 3;
                    effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(GraphicsDevice.ImmediateContext);
                    GraphicsDevice.ImmediateContext.DrawIndexed(indexCount, startIdx, 0);
                }
                startIdx += indexCount;
            }

            effect.GetVariableByName("tex").AsScalar().Set(false);
            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(GraphicsDevice.ImmediateContext);
            GraphicsDevice.ImmediateContext.DrawIndexed(indices.Count, 0, 0);
        }

        /// <summary>
        /// マテリアル名から一致するマテリアルを調べる
        /// </summary>
        /// <param name="materialReference">参照マテリアル名</param>
        /// <returns>参照先マテリアル</returns>
        private Material GetMaterialFromReference(string materialReference)
        {
            foreach(Material m in cnv.matList.materials)
            {
                if (m.Name.Equals(materialReference))
                    return m;
            }
            return null;
        }

        protected override void LoadContent()
        {
            InitXConverter();

            InitEffect();
            InitVertexLayout();
            InitVertexBuffer();
            InitIndexBuffer();
            InitTexture();
        }

        private void InitXConverter()
        {
            cnv = new XFileConverter(filePath);
            filePath = cnv.FilePath;
        }

        private void InitEffect()
        {
            using (D3DComp.ShaderBytecode shaderBytecode = D3DComp.ShaderBytecode.CompileFromFile(
                "MyEffect.fx", "fx_5_0",
                D3DComp.ShaderFlags.None,
                D3DComp.EffectFlags.None
                ))
            {
                effect = new Dx11.Effect(GraphicsDevice, shaderBytecode);
            }
        }

        private void InitVertexLayout()
        {
            vertexLayout = new Dx11.InputLayout(
                GraphicsDevice,
                effect.GetTechniqueByIndex(0).GetPassByIndex(0).Description.Signature, VertexPositionTexture.VertexElements);
        }

        /// <summary>
        /// 頂点情報の格納
        /// </summary>
        private void InitVertexBuffer()
        {
            for(int i=0; i<cnv.meshSection.vtxList.VertexCount; i++)
            {
                Vector3 vtxPos = cnv.meshSection.vtxList.vertex[i];
                Vector2 uvPos = cnv.meshSection.uvList.uvs[i];

                Console.WriteLine("AddVertex ["+ i + "] : x:" + vtxPos.X + ",y:" + vtxPos.Y + ",z:" + vtxPos.Z);
                Console.WriteLine("AddUv [" + i + "] : x:" + uvPos.X + ",y:" + uvPos.Y);

                vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(vtxPos, uvPos));
            }

            vertexBuffer = MyDirectXHelper.CreateVertexBuffer(GraphicsDevice, vtxList.ToArray());

        }

        /// <summary>
        /// インデックスの格納
        /// </summary>
        private void InitIndexBuffer()
        {
            int counter = 0;
            foreach(int[] meshes in cnv.meshSection.meshList.mesh)
            {
                Console.WriteLine("index:" + counter);
                if(meshes.Length == VTXNUM_SQUARE)
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
            indexBuffer = MyDirectXHelper.CreateIndexBuffer(GraphicsDevice, indices.ToArray());
            Console.WriteLine("-----IndexBuffer-----");
            for(int i=0; i<indices.Count; i++)
            {
                Console.WriteLine("indices[" + i + "]=" + indices[i]);
            }
        }

        /// <summary>
        /// テクスチャをデバイスに代入
        /// </summary>
        private void InitTexture()
        {
            string directoryPath = System.IO.Path.GetDirectoryName(filePath) + "/";

            //テクスチャの抽出
            for(int i = 0; i < cnv.matList.MaterialCount; i++)
            {
                if (!cnv.matList.materials[i].TextureFileName.Equals(""))
                {
                    //テクスチャ登録
                    try
                    {
                        textureMan.SetTexture(
                            cnv.matList.materials[i].Name,
                            Dx11.ShaderResourceView.FromFile(
                                GraphicsDevice, directoryPath + cnv.matList.materials[i].TextureFileName,
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
                        Console.WriteLine("Texture:" + cnv.matList.materials[i].TextureFileName + " is not found.");
                        continue;
                    }
                }
            }
        }

        protected override void UnloadContent()
        {
            effect.Dispose();
            foreach (var t in textureMan.GetAllTextures()) t?.Dispose();
            vertexLayout.Dispose();
            vertexBuffer.Dispose();
            indexBuffer.Dispose();
        }

        private void InitializeComponent()
        {
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ファイルFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ファイルFToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(684, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ファイルFToolStripMenuItem
            // 
            this.ファイルFToolStripMenuItem.Name = "ファイルFToolStripMenuItem";
            this.ファイルFToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
            this.ファイルFToolStripMenuItem.Text = "ファイル(&F)";
            // 
            // View
            // 
            this.ClientSize = new System.Drawing.Size(684, 511);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "View";
            this.Text = "Test";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

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

    class TextureManagement
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
    }
}
