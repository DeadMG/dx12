namespace Renderer.Direct3D12.Shaders
{
    internal interface IRaytracingPipelineStep : IDisposable
    {
        public Vortice.Direct3D12.StateSubObject[] CreateStateObjects();

        public void PrepareRaytracing(RaytracePreparation prep);
        public void CommitRaytracing(RaytraceCommit commit);
    }
}
