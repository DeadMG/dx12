namespace Renderer.Direct3D12.Shaders
{
    public class Emitter
    {
        private readonly List<CompilationResult> compilationResults = new List<CompilationResult>();

        public void Add(CompilationResult result)
        {
            compilationResults.Add(result);
        }

        public EmittedFile[] Emit()
        {
            var files = new List<EmittedFile>();

            var typeLookup = new Emit.Type().Emit(compilationResults, files);

            foreach (var file in compilationResults)
            {
                files.Add(new EmittedFile
                {
                    Contents = new Emit.DXIL().GenerateLibrary(file, typeLookup),
                    RelativePath = Path.ChangeExtension(file.Path, ".g.cs")
                });
            }

            return files.ToArray();
        }
    }

    public class EmittedFile
    {
        public required string RelativePath { get; init; }
        public required string Contents { get; init; }
    }
}
