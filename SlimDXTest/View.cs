using System;
using System.Collections.Generic;
using SlimDX;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using D3DComp = SlimDX.D3DCompiler;
using SlimDX.RawInput;

namespace SlimDXTest
{
    class View : Core
    {
        Dx11.Effect effect;
        List<Model> models = new List<Model>();

        Vector3 moving = new Vector3(0,0,0);
        float zooming = 0;

        /// <summary>
        /// マウス入力の受け取り
        /// </summary>
        protected override void MouseInput(object sender, MouseInputEventArgs e)
        {
            if (e.WheelDelta > 0)
                zooming += 0.1f;
            else if (e.WheelDelta < 0)
                zooming -= 0.1f;
        }

        /// <summary>
        /// キーボード入力の受け取り
        /// </summary>
        protected override void KeyInput(object sender, KeyboardInputEventArgs e)
        {
            //距離移動
            if(e.Key == System.Windows.Forms.Keys.Up)
                moving.Z += 1f;
            if(e.Key == System.Windows.Forms.Keys.Down)
                moving.Z -= 1f;

            //上下左右移動
            if (e.Key == System.Windows.Forms.Keys.W)
                moving.Y -= 1f;
            if (e.Key == System.Windows.Forms.Keys.S)
                moving.Y += 1f;
            if (e.Key == System.Windows.Forms.Keys.D)
                moving.X -= 1f;
            if (e.Key == System.Windows.Forms.Keys.A)
                moving.X += 1f;
        }

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

            foreach (Model model in models)
            {
                model.InitModelInputAssembler(GraphicsDevice);
                model.DrawModel(GraphicsDevice, effect);
            }
            SwapChain.Present(0, Dxgi.PresentFlags.None);
        }

        /// <summary>
        /// 視点処理
        /// </summary>
        private void UpdateCamera()
        {
            var world = Matrix.Identity;

            var viewEye = new Vector3(0, 5, 45);
            var viewTarget = new Vector3(0, 5, 0);
            var view = Matrix.Multiply(
                Matrix.Multiply(
                    Matrix.RotationX(0),
                    Matrix.RotationY(0)
                ), Matrix.Multiply(
                    Matrix.LookAtRH(viewEye, viewTarget, new Vector3(0, 1, 0)),
                    Matrix.Translation(moving.X, moving.Y, moving.Z * 5.5f)
                )
            );

            if (zooming < 0)
                zooming = 0;
            else if (zooming > 3)
                zooming = 3;

            var projection = Matrix.PerspectiveFovRH(
                30 * ((float)Math.PI - zooming) / 180, (ClientSize.Width - zooming) / ClientSize.Height, 0.1f, 1000
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
            models[0].Position = new Vector3(10, 0, 0);
            models.Add(new Model("./Bill1.x"));

            foreach (Model model in models)
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
