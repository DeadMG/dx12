using Renderer;

namespace Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await new Initialiser().Run();
            var window = new Window();
            await window.Closed;
        }
    }
}
