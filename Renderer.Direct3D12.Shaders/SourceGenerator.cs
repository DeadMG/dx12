using Renderer.Direct3D12.Shaders.ShaderModel;
using System.Diagnostics;

namespace Renderer.Direct3D12.Shaders
{
    public class SourceGenerator
    {
        public static int Main(string[] args)
        {
            var model = new Generator();
            model.LoadDxil("Raytrace/Hit/Object.hlsl");
            model.LoadDxil("Raytrace/Miss/Black.hlsl");
            model.LoadDxil("Raytrace/RayGen/Camera.hlsl");
            return 1;
        }
    }
}
