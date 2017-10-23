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
        Dx11.Effect effect;
        List<Model> models = new List<Model>();

        /// <summary>
        /// 描画処理
        /// </summary>
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

            //foreach(Model model in models)
            //{
            //    model.InitModelInputAssembler(GraphicsDevice);
            //    model.DrawModel(GraphicsDevice, effect);

            //}
            models[0].InitModelInputAssembler(GraphicsDevice);
            models[0].DrawModel(GraphicsDevice, effect);

            SwapChain.Present(0, Dxgi.PresentFlags.None);
        }

        /// <summary>
        /// 視点処理
        /// </summary>
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

        protected override void LoadContent()
        {
            InitEffect();
            InitModels();
        }

        private void InitModels()
        {
            //テスト
            models.Add(new Model("./244End.x"));
            foreach(Model model in models)
            {
                model.Init(GraphicsDevice, effect);
            }
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

        protected override void UnloadContent()
        {
            effect.Dispose();
            foreach(Model model in models)
            {
                model.Unload();
            }
        }
    }
}
