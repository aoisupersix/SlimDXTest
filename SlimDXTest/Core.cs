using SlimDX;
using SlimDX.Windows;
using System.Windows.Forms;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;

namespace SlimDXTest
{
    class Core : Form
    {
        public Dx11.Device GraphicsDevice;
        public Dxgi.SwapChain SwapChain;
        public Dx11.RenderTargetView RenderTarget;
        public Dx11.DepthStencilView DepthStencil;

        public void Run()
        {
            InitDevice();
            MessagePump.Run(this, Draw);
            DisposeDevice();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        private void InitDevice()
        {
            MyDirectXHelper.CreateDeviceAndSwapChain(this, out GraphicsDevice, out SwapChain);
            InitRasterizerState();
            InitRenderTarget();
            InitDepthStencil();
            GraphicsDevice.ImmediateContext.OutputMerger.SetTargets(DepthStencil, RenderTarget);
            InitViewport();

            LoadContent();
        }

        private void InitRasterizerState()
        {
            GraphicsDevice.ImmediateContext.Rasterizer.State = Dx11.RasterizerState.FromDescription(
                GraphicsDevice,
                new Dx11.RasterizerStateDescription()
                {
                    CullMode = Dx11.CullMode.None,
                    FillMode = Dx11.FillMode.Solid
                }
            );
        }

        private void InitRenderTarget()
        {
            using (Dx11.Texture2D backBuffer
                = Dx11.Resource.FromSwapChain<Dx11.Texture2D>(SwapChain, 0)
                )
            {
                RenderTarget = new Dx11.RenderTargetView(GraphicsDevice, backBuffer);
                GraphicsDevice.ImmediateContext.OutputMerger.SetTargets(RenderTarget);
            }
        }

        private void InitDepthStencil()
        {
            Dx11.Texture2DDescription depthBufferDesc = new Dx11.Texture2DDescription
            {
                ArraySize = 1,
                BindFlags = Dx11.BindFlags.DepthStencil,
                Format = Dxgi.Format.D32_Float,
                Width = ClientSize.Width,
                Height = ClientSize.Height,
                MipLevels = 1,
                SampleDescription = new Dxgi.SampleDescription(1, 0)
            };

            using (Dx11.Texture2D depthBuffer = new Dx11.Texture2D(GraphicsDevice, depthBufferDesc))
            {
                DepthStencil = new Dx11.DepthStencilView(GraphicsDevice, depthBuffer);
            }
        }

        private void InitViewport()
        {
            GraphicsDevice.ImmediateContext.Rasterizer.SetViewports(
                new Dx11.Viewport()
                {
                    Width = ClientSize.Width,
                    Height = ClientSize.Height,
                    MaxZ = 1
                }
                );
        }

        private void DisposeDevice()
        {
            UnloadContent();
            RenderTarget.Dispose();
            GraphicsDevice.Dispose();
            SwapChain.Dispose();
        }

        protected virtual void Draw() { }
        protected virtual void LoadContent() { }
        protected virtual void UnloadContent() { }
    }
}