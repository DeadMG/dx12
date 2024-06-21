using Renderer.Direct3D12.Shaders.ShaderModel;
using System.Diagnostics;

namespace Renderer.Direct3D12.Shaders
{
    public class SourceGenerator
    {
        private readonly Generator model = new Generator();

        public SourceGenerator()
        {
            model.LoadDxil("Raytrace/Hit/Object.hlsl");
            model.LoadDxil("Raytrace/Hit/Sun.hlsl");
            model.LoadDxil("Raytrace/Miss/Black.hlsl");
            model.LoadDxil("Raytrace/RayGen/Camera.hlsl");
        }

        public static int Main(string[] args)
        {
            return 1;
        }
    }
}
