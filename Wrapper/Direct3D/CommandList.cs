namespace Wrapper.Direct3D
{
    public class CommandList
    {
        protected readonly CommandQueue queue;

        private SharpDX.Direct3D12.GraphicsCommandList? commandList;
        private SharpDX.Direct3D12.CommandAllocator? commandAllocator;

        internal CommandList(CommandQueue queue)
        {
            this.queue = queue;
        }

        public FenceWait Execute()
        {
            var waitValue = queue.Execute(commandList, commandAllocator);
            commandList = null;
            return waitValue;
        }

        public void Barrier(Resource resource, SharpDX.Direct3D12.ResourceStates origin, SharpDX.Direct3D12.ResourceStates destination)
        {
            List.ResourceBarrier(new SharpDX.Direct3D12.ResourceBarrier(new SharpDX.Direct3D12.ResourceTransitionBarrier(resource.Native, origin, destination)));
        }

        protected SharpDX.Direct3D12.GraphicsCommandList List
        {
            get
            {
                if (commandList == null)
                {
                    var tuple = queue.GetCommandList();
                    commandList = tuple.Item1;
                    commandAllocator = tuple.Item2;
                }

                return commandList;
            }
        }
    }
}
