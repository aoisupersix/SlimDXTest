using SlimDX;
using SlimDX.Windows;
using System.Windows.Forms;
using Dx11 = SlimDX.Direct3D11;
using Dxgi = SlimDX.DXGI;
using SlimDX.Multimedia;
using Rwin = SlimDX.RawInput;

namespace SlimDXTest
{
    class Core : Form
    {
        public Dx11.Device GraphicsDevice;
        public Dxgi.SwapChain SwapChain;
        public Dx11.RenderTargetView RenderTarget;
        public Dx11.DepthStencilView DepthStencil;
        private Panel ViewPanel;
        private MenuStrip menuStrip1;
        private ToolStripMenuItem ファイルFToolStripMenuItem;
        public Label PositionLabel;

        public void Run()
        {
            InitDevice();
            MessagePump.Run(this, Draw);
            DisposeDevice();
        }

        /// <summary>
        /// 初期化
        /// </summary>
        protected virtual void InitDevice()
        {
            InitializeComponent();
            MyDirectXHelper.CreateDeviceAndSwapChain(this, ViewPanel, out GraphicsDevice, out SwapChain);

            InitRasterizerState();
            InitRenderTarget();
            InitDepthStencil();
            GraphicsDevice.ImmediateContext.OutputMerger.SetTargets(DepthStencil, RenderTarget);
            InitViewport();
            InitInputDevice();

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

        private void InitInputDevice()
        {
            Rwin.Device.RegisterDevice(UsagePage.Generic, UsageId.Mouse, Rwin.DeviceFlags.None);
            Rwin.Device.MouseInput += MouseInput;
            Rwin.Device.RegisterDevice(UsagePage.Generic, UsageId.Keyboard, Rwin.DeviceFlags.None);
            Rwin.Device.KeyboardInput += KeyInput;
        }

        private void DisposeDevice()
        {
            UnloadContent();
            RenderTarget.Dispose();
            GraphicsDevice.ImmediateContext.Rasterizer.State.Dispose();
            GraphicsDevice.Dispose();
            SwapChain.Dispose();
        }

        protected virtual void Draw() { }
        protected virtual void LoadContent() { }
        protected virtual void UnloadContent() { }
        protected virtual void MouseInput(object sender, Rwin.MouseInputEventArgs e) { }
        protected virtual void KeyInput(object sender, Rwin.KeyboardInputEventArgs e) { }

        private void InitializeComponent()
        {
            this.ViewPanel = new System.Windows.Forms.Panel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.ファイルFToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.ViewPanel.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // ViewPanel
            // 
            this.ViewPanel.Controls.Add(this.menuStrip1);
            this.ViewPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ViewPanel.Location = new System.Drawing.Point(0, 0);
            this.ViewPanel.Name = "ViewPanel";
            this.ViewPanel.Size = new System.Drawing.Size(800, 600);
            this.ViewPanel.TabIndex = 0;
            // 
            // menuStrip1
            // 
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.ファイルFToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(800, 24);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // ファイルFToolStripMenuItem
            // 
            this.ファイルFToolStripMenuItem.Name = "ファイルFToolStripMenuItem";
            this.ファイルFToolStripMenuItem.Size = new System.Drawing.Size(67, 20);
            this.ファイルFToolStripMenuItem.Text = "ファイル(&F)";
            // 
            // Core
            // 
            this.ClientSize = new System.Drawing.Size(800, 600);
            this.Controls.Add(this.ViewPanel);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "Core";
            this.ViewPanel.ResumeLayout(false);
            this.ViewPanel.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);

        }
    }
}