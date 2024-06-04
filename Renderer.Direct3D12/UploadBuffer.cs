using System.Drawing;
using System.Numerics;
using Util;

namespace Renderer.Direct3D12
{
    public class UploadBuffer : IDisposable
    {
        private readonly SharpDX.Direct3D12.Resource buffer;

        public UploadBuffer(SharpDX.Direct3D12.Device device, int size)
        {
            buffer = device.CreateCommittedResource(new SharpDX.Direct3D12.HeapProperties(SharpDX.Direct3D12.HeapType.Upload),
                    SharpDX.Direct3D12.HeapFlags.None,
                    SharpDX.Direct3D12.ResourceDescription.Buffer(new SharpDX.Direct3D12.ResourceAllocationInformation { Alignment = 65536, SizeInBytes = size }),
                    SharpDX.Direct3D12.ResourceStates.GenericRead);
        }

        public void Dispose()
        {
            buffer.Dispose();
        }
    }
}
