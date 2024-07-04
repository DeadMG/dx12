namespace Renderer.Direct3D12.Shaders
{
    public class Emitter
    {
        private readonly List<CompilationResult> compilationResults = new List<CompilationResult>();

        public void Add(CompilationResult result)
        {
            if (result == null) return;
            compilationResults.Add(result);
        }

        public EmittedFile[] Emit()
        {
            var files = new List<EmittedFile>();

            var typeLookup = new Emit.Type().Emit(compilationResults, files);

            foreach (var file in compilationResults)
            {
                if (file.Type == EmitType.DXR)
                {
                    files.Add(new EmittedFile
                    {
                        Contents = new Emit.DXR().GenerateLibrary(file, typeLookup),
                        RelativePath = Path.ChangeExtension(file.Path, ".g.cs")
                    });
                    continue;
                }

                if (file.Type == EmitType.Compute)
                {
                    files.Add(new EmittedFile
                    {
                        Contents = new Emit.Compute().GenerateShader(file, typeLookup),
                        RelativePath = Path.ChangeExtension(file.Path, ".g.cs")
                    });
                    continue;
                }

                if (file.Type == EmitType.None)
                {
                    continue;
                }

                throw new InvalidOperationException();
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
