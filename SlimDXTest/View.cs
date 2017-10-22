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
        public string filePath = "./Teimoku.x";

        Dx11.Effect effect;
        Dx11.InputLayout vertexLayout;
        Dx11.Buffer vertexBuffer;

        List<VertexPositionTexture> vtxList = new List<VertexPositionTexture>();
        Dx11.ShaderResourceView[] texture;

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
            double time = System.Environment.TickCount / 500d;

            Matrix world = Matrix.LookAtRH(
                new Vector3((float)System.Math.Cos(time), 0, (float)System.Math.Sin(time)),
                new Vector3(),
                new Vector3(0, 1, 0)
                );

            var view = Matrix.LookAtRH(
                new Vector3(0, 10, -10), new Vector3(0, 10, 0), new Vector3(0, 1, 0)
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
            vertexBuffer = MyDirectXHelper.CreateVertexBuffer(GraphicsDevice, vtxList.ToArray());

            GraphicsDevice.ImmediateContext.InputAssembler.InputLayout = vertexLayout;
            GraphicsDevice.ImmediateContext.InputAssembler.SetVertexBuffers(
                0,
                new Dx11.VertexBufferBinding(vertexBuffer, VertexPositionTexture.SizeInBytes, 0)
                );
            //GraphicsDevice.ImmediateContext.InputAssembler.SetVertexBuffers(0, buffers);

            GraphicsDevice.ImmediateContext.InputAssembler.PrimitiveTopology
                = Dx11.PrimitiveTopology.TriangleList;
        }

        private void DrawModel()
        {
            effect.GetTechniqueByIndex(0).GetPassByIndex(0).Apply(GraphicsDevice.ImmediateContext);
            GraphicsDevice.ImmediateContext.Draw(vtxList.Count, 0);
        }

        protected override void LoadContent()
        {
            InitXConverter();

            InitEffect();
            InitVertexLayout();
            InitVertexBuffer();
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
            const int VERTICES_SQUARE = 4;
            int counter = 0;
            foreach (int[] mesh in cnv.meshSection.meshList.mesh)
            {
                Console.WriteLine("Mesh:" + counter);
                if (mesh.Length == VERTICES_SQUARE)
                {
                    Console.WriteLine("V:SQUARE");
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[0]], cnv.meshSection.uvList.uvs[mesh[0]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[1]], cnv.meshSection.uvList.uvs[mesh[1]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[3]], cnv.meshSection.uvList.uvs[mesh[3]]));

                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[1]], cnv.meshSection.uvList.uvs[mesh[1]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[2]], cnv.meshSection.uvList.uvs[mesh[2]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[3]], cnv.meshSection.uvList.uvs[mesh[3]]));
                }
                else
                {
                    Console.WriteLine("V:TRIANGLE");
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[0]], cnv.meshSection.uvList.uvs[mesh[0]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[1]], cnv.meshSection.uvList.uvs[mesh[1]]));
                    vtxList.Add(MyDirectXHelper.CreateVertexPositionTexture(cnv.meshSection.vtxList.vertex[mesh[2]], cnv.meshSection.uvList.uvs[mesh[2]]));
                }
                foreach (int m in mesh)
                {
                    Console.Write(m + ",");
                }
                Console.WriteLine();
            }

            foreach (VertexPositionTexture v in vtxList)
            {
                Vector3 pos = v.Position;
                Console.WriteLine("-----Vertex------");
                Console.WriteLine("x:" + pos.X + ",y:" + pos.Y + ",z:" + pos.Z);
            }
            Console.ReadLine();
        }

        /// <summary>
        /// テクスチャをデバイスに代入
        /// </summary>
        private void InitTexture()
        {
            //テクスチャの抽出
            List<Material> texMat = new List<Material>();
            foreach (Material mat in cnv.matList.materials)
            {
                if (!mat.TextureFileName.Equals(""))
                    texMat.Add(mat);
            }

            string directoryPath = System.IO.Path.GetDirectoryName(filePath) + "/";

            //テクスチャの登録
            texture = new Dx11.ShaderResourceView[texMat.Count];
            for (int i = 0; i < texture.Length; i++)
            {
                try
                {
                    texture[i] = Dx11.ShaderResourceView.FromFile(GraphicsDevice, directoryPath + texMat[i].TextureFileName);
                }
                catch
                {
                    Console.WriteLine("Texture:" + texMat[i].TextureFileName + " is not found.");
                    continue;
                }
            }
        }

        protected override void UnloadContent()
        {
            effect.Dispose();
            vertexLayout.Dispose();
            vertexBuffer.Dispose();
        }
    }

    /// <summary>
    /// 各頂点の色情報
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
