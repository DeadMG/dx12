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

            var simulation = new Game();
            var player = simulation.AddPlayer(simulation.AddForce());
            var controlScheme = new StandardControlScheme(player, 1, 1);

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

            var world = simulation.AddWorld();
            world.Units.Add(new Unit
            {
                Orientation = new Quaternion(),
                Position = new Vector3(8, 0, 8),
                Blueprint = print,
                Player = player,
            });
            world.Units.Add(new Unit
            {
                Orientation = new Quaternion(),
                Position = new Vector3(-8, 0, 8),
                Blueprint = print,
                Player = player,
            });
            world.Units.Add(new Unit
            {
                Orientation = new Quaternion(),
                Position = new Vector3(8, 0, -8),
                Blueprint = print,
                Player = player,
            });
            world.Units.Add(new Unit
            {
                Orientation = new Quaternion(),
                Position = new Vector3(-8, 0, -8),
                Blueprint = print,
                Player = player,
            });

            player.CameraFor(world).Position = new Vector3(0, 30, 0);
            player.CameraFor(world).Orientation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), 90f.ToRadians());

            var queue = new ResizeTracker();
            var window = new Window() { Listener = new WindowListener(queue, controlScheme) };

            var hwnd = await window.HWND;
            var size = await window.InitialSize;

            controlScheme.OnResize(size.Width, size.Height);

            using (var renderer = new Core(hwnd, size.Width, size.Height))
            using (var cts = new CancellationTokenSource())
            {
                await renderer.Load([print]);

                var renderLoop = CoreLoop(renderer, queue, simulation, player, controlScheme, cts.Token);

                await window.Closed;
                cts.Cancel();
                await renderLoop;
            }
        }

        static async Task CoreLoop(Core core, ResizeTracker queue, Game simulation, Player player, IControlScheme controlScheme, CancellationToken token)
        {
            var frameCount = 0;

            var simWatch = new SimWatch();

            var renderWatch = new Stopwatch();
            renderWatch.Start();

            while (!token.IsCancellationRequested)
            {
                if (queue.DidResize(out var resize))
                {
                    core.Resize(resize.Width, resize.Height);
                    controlScheme.OnResize(resize.Width, resize.Height);
                }

                controlScheme.Apply(simulation);
                var renderTask = core.Render(simulation, player);
                await simulation.Update(simWatch.MarkTime());
                await renderTask;
                frameCount = frameCount + 1;
            }

            Debug.Write($"FPS: {frameCount / renderWatch.Elapsed.TotalSeconds}\n");
            new Debugging().ReportLiveObjects();
        }
    }
}
