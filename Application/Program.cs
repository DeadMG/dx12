using Application;
using Data;
using Renderer;
using Simulation;
using System.Diagnostics;
using System.Drawing;
using System.Numerics;

namespace Test
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            await new Initialiser().Run();

            var queue = new InputQueue();
            var window = new Window() { Listener = new QueueWindowListener(queue) };
            var hwnd = await window.HWND;
            var size = await window.InitialSize;

            using (var renderer = new Core(hwnd, size.Width, size.Height))
            using (var cts = new CancellationTokenSource()) 
            {
                var renderLoop = CoreLoop(renderer, queue, cts.Token);

                await window.Closed;
                cts.Cancel();
                await renderLoop;
            }
        }

        static async Task CoreLoop(Core core, InputQueue queue, CancellationToken token)
        {
            var frameCount = 0;
            var sw = new Stopwatch();
            sw.Start();

            var alliance = new Force();
            var player = new Player { Force = alliance };
            var print = new Blueprint
            {
                Name = "Hypercraft",
                Mesh = new Mesh
                {
                    Vertices =
                    [
                        new Vertex { Position = new Vector3(3.0f, 0.0f, 0.0f), Colour = new Colour { R = 0, G = 1, B = 0 } },
                        new Vertex { Position = new Vector3(0.0f, 3.0f, -3.0f), Colour = new Colour { R = 0, G = 0, B = 1 }, },
                        new Vertex { Position = new Vector3(0.0f, 0.0f, 10.0f), Colour = new Colour { R = 1, G = 0, B = 0 }, },
                        new Vertex { Position = new Vector3(-3.0f, 0.0f, 0.0f), Colour = new Colour { R = 0, G = 1, B = 1 }, },
                    
                        // left gun
                        new Vertex { Position = new Vector3(3.2f, -1.0f, -3.0f), Colour = new Colour { R = 0, G = 0, B = 1 }, },
                        new Vertex { Position = new Vector3(3.2f, -1.0f, 11.0f), Colour = new Colour { R = 0, G = 1, B = 0 }, },
                        new Vertex { Position = new Vector3(2.0f, 1.0f, 2.0f), Colour = new Colour { R = 1, G = 0, B = 0 }, },
                    
                        // right gun
                        new Vertex { Position = new Vector3(-3.2f, -1.0f, -3.0f), Colour = new Colour { R = 0, G = 0, B = 1 }, },
                        new Vertex { Position = new Vector3(-3.2f, -1.0f, 11.0f), Colour = new Colour { R = 0, G = 1, B = 0 }, },
                        new Vertex { Position = new Vector3(-2.0f, 1.0f, 2.0f), Colour = new Colour { R = 1, G = 0, B = 0 }, },
                    ],
                    Indices = 
                    [
                        0, 1, 2,
                        2, 1, 3,
                        3, 1, 0,
                        0, 2, 3,
                        4, 5, 6,
                        7, 8, 9,
                    ]
                }
            };

            var scene = new World
            {
                CameraOrientation = new Quaternion(),
                CameraPosition = new Vector3(),
                Units =
                {
                    new Unit
                    {
                        Orientation = new Quaternion(),
                        Position = new Vector3(0, 0f, -50f),
                        Blueprint = print,
                        Player = player,
                    }
                }
            };

            await core.Load([print]);

            while (!token.IsCancellationRequested)
            {
                if (queue.DidResize(out var resize))
                {
                    core.Resize(resize.Width, resize.Height);
                }

                scene = await Update(scene, sw.Elapsed);
                await core.Render(scene);
                frameCount = frameCount + 1;
            }

            Debug.Write($"FPS: {frameCount / sw.Elapsed.TotalSeconds}\n");
            new Debugging().ReportLiveObjects();
        }

        static async Task<World> Update(World scene, TimeSpan time)
        {
            scene.Units[0].Orientation = Quaternion.CreateFromAxisAngle(new Vector3(0, 1, 0), (float)time.TotalSeconds * (float)Math.PI);
            return scene;
        }
    }
}
