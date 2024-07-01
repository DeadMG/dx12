namespace Renderer.Direct3D12.Shaders
{
    public class SourceGenerator
    {
        public static int Main(string[] args)
        {
            var emitter = new Emitter();
            var model = new Generator();

            emitter.Add(model.LoadDxil("Shaders/Raytrace/Hit/ObjectRadiance.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Hit/ObjectShadow.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Hit/SphereIntersection.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Hit/SphereRadiance.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Hit/SphereShadow.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Miss/Black.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/Miss/Starfield.hlsl"));
            emitter.Add(model.LoadDxil("Shaders/Raytrace/RayGen/Camera.hlsl"));

            foreach (var file in emitter.Emit())
            {
                var path = Path.Combine(args[0], file.RelativePath);
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                File.WriteAllText(path, file.Contents);
            }

            return 0;
        }
    }
}
