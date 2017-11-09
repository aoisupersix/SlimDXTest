using SlimDX.DXGI;
using SlimDX.Direct3D11;
using SlimDX;

namespace SlimDXTest
{
    /// <summary>
    /// DirectXの処理をまとめたもの
    /// </summary>
    class MyDirectXHelper
    {
        //デバイスとスワップチェーンの初期化
        public static void CreateDeviceAndSwapChain(
            System.Windows.Forms.Form form,
            System.Windows.Forms.Panel panel,
            out SlimDX.Direct3D11.Device device,
            out SwapChain swapChain
            )
        {
            SlimDX.Direct3D11.Device.CreateWithSwapChain(
                DriverType.Hardware,
                DeviceCreationFlags.None,
                new SwapChainDescription
                {
                    BufferCount = 1,
                    OutputHandle = panel.Handle,
                    IsWindowed = true,
                    SampleDescription = new SampleDescription
                    {
                        Count = 1,
                        Quality = 0
                    },
                    ModeDescription = new ModeDescription
                    {
                        Width = form.ClientSize.Width,
                        Height = form.ClientSize.Height,
                        RefreshRate = new SlimDX.Rational(60, 1),
                        Format = Format.R8G8B8A8_UNorm
                    },
                    Usage = Usage.RenderTargetOutput
                },
                out device,
                out swapChain
                );
        }

        public static Buffer CreateVertexBuffer(SlimDX.Direct3D11.Device graphicsDevice, System.Array vertices)
        {
            using (SlimDX.DataStream vertexStream = new SlimDX.DataStream(vertices, true, true))
            {
                return new Buffer(
                    graphicsDevice,
                    vertexStream,
                    new BufferDescription
                    {
                        SizeInBytes = (int)vertexStream.Length,
                        BindFlags = BindFlags.VertexBuffer,
                    }
                    );
            }
        }

        public static Buffer CreateIndexBuffer(SlimDX.Direct3D11.Device graphicsDevice, uint[] indices)
        {
            using (SlimDX.DataStream indicesStream = new SlimDX.DataStream(indices, true, true))
            {
                return new Buffer(
                    graphicsDevice,
                    indicesStream,
                    new BufferDescription
                    {
                        SizeInBytes = (int)indicesStream.Length,
                        BindFlags = BindFlags.IndexBuffer,
                        StructureByteStride = sizeof(uint)
                    }
                    );
            }
        }

        public static VertexPositionTexture CreateVertexPositionTexture(Vector3 pos, Vector2 texCoord)
        {
            float scale = 0.5f;
            float x = pos.X, y = pos.Y, z = pos.Z;
            VertexPositionTexture vtx = new VertexPositionTexture
            {
                Position = new Vector3(x / scale, y / scale, z / scale),
                TextureCoordinate = texCoord
            };
            return vtx;
        }

        private static float floatGating(float num)
        {
            if (num > 1)
                num = 1;
            else if (num < -1)
                num = -1;
            return num;
        }
    }
}
