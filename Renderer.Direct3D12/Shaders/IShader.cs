namespace Renderer.Direct3D12.Shaders
{
    internal interface IShader : IDisposable
    {
        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects();
        public string[] Exports { get; }

        public void PrepareRaytracing(RaytracePreparation prep);
        public void FinaliseRaytracing(RaytraceFinalisation finalise);
        public void CommitRaytracing(RaytraceCommit commit);
    }
}
