namespace Renderer.Direct3D12.Shaders
{
    public class SourceGenerator
    {
        public static int Main(string[] args)
        {
            var emitter = new Emitter();
            var model = new Generator();

            var files = new DirectoryInfo("Shaders").GetFiles("*.hlsl", SearchOption.AllDirectories);

            foreach (var inputFile in files)
            {
                if (inputFile.Directory.FullName.Contains("PIX")) continue;
                emitter.Add(model.TryLoad(Path.GetRelativePath(Directory.GetCurrentDirectory(), inputFile.FullName)));
            }

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
