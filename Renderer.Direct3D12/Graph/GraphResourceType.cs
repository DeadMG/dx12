namespace Renderer.Direct3D12.Graph
{
    /*
Broadly, resources should be classified into four categories.
* Resources that are written once, and live forever.
** e.g. mesh data.
* Resources that are written on the CPU timeline, then read on the GPU timeline.
** e.g. instance data
* Resources that are written and read on the GPU timeline of that frame.
** e.g. acceleration structures, textures
* Resources that are written and read on the GPU timeline of multiple frames.
** history textures/buffers

Readonly resources can be classified as Permanent, the same resource is re-used forever.
Resources written and read on the GPU timeline of that frame can also be classified as Permanent because every frame can reference the same resource.
Resources written on the CPU timeline and read on the GPU timeline are per-frame, i.e. if you have N frames in flight, you need N of these.
Resources written on the GPU timeline, then read on the GPU timeline of H frames need H copies used in a ring buffer. Note that H includes current frame.

We therefore define resources in four classifications:
* Permanent
* Per-frame
* History
* Special (e.g. swapchain back buffer)

These concepts also apply to D3D objects like command lists, command allocators, and devices. However in practice only items that need to be read by 
the GPU actually need to be considered; so devices probably don't need to count.
     */
    internal enum GraphResourceType
    {
        Permanent,
        PerFrame,
        History,
        Special
    }
}
