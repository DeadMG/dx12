using Data.Space;
using Platform.Contracts;
using Simulation;
using System.Diagnostics;
using System.Numerics;
using UI;

namespace Application
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var platform = new PlatformSelector().GetPlatform();

            await platform.OneTimeInitialisation();

            var scenario = new Simulation.Scenario();
            var player = scenario.AddPlayer(scenario.AddForce());

            var print = new Simulation.Blueprint
            {
                Name = "Hypercraft",
                Mesh = new Simulation.Mesh
                {
                    Vertices =
                    [
                        new Simulation.Vertex { Position = new Vector3(3.0f, 0.0f, 0.0f), Colour = new RGB { R = 0, G = 1, B = 0 } },
                        new Simulation.Vertex { Position = new Vector3(0.0f, 3.0f, -3.0f), Colour = new RGB { R = 0, G = 0, B = 1 }, }, 
                        new Simulation.Vertex { Position = new Vector3(0.0f, 0.0f, 10.0f), Colour = new RGB { R = 1, G = 0, B = 0 }, },
                        new Simulation.Vertex { Position = new Vector3(-3.0f, 0.0f, 0.0f), Colour = new RGB { R = 0, G = 1, B = 1 }, },
                    
                        // left gun
                        new Simulation.Vertex { Position = new Vector3(3.2f, -1.0f, -3.0f), Colour = new RGB { R = 0, G = 0, B = 1 }, },
                        new Simulation.Vertex { Position = new Vector3(3.2f, -1.0f, 11.0f), Colour = new RGB { R = 0, G = 1, B = 0 }, },
                        new Simulation.Vertex { Position = new Vector3(2.0f, 1.0f, 2.0f), Colour = new RGB { R = 1, G = 0, B = 0 }, },
                    
                        // right gun
                        new Simulation.Vertex { Position = new Vector3(-3.2f, -1.0f, -3.0f), Colour = new RGB { R = 0, G = 0, B = 1 }, },
                        new Simulation.Vertex { Position = new Vector3(-3.2f, -1.0f, 11.0f), Colour = new RGB { R = 0, G = 1, B = 0 }, },
                        new Simulation.Vertex { Position = new Vector3(-2.0f, 1.0f, 2.0f), Colour = new RGB { R = 1, G = 0, B = 0 }, },
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
                },
                Acceleration = 3,
                MaxSpeed = 100,
                TurnRate = (float)Math.PI / 2,
            };

            var volume = scenario.AddVolume(new Vector3(100000, 100000, 100000));

            volume.Add(new Simulation.Unit(player, print, new Vector3(8, 0, 8), Quaternion.Identity));
            volume.Add(new Simulation.Unit(player, print, new Vector3(-8, 0, 8), Quaternion.Identity));
            volume.Add(new Simulation.Unit(player, print, new Vector3(-8, 0, -8), Quaternion.Identity));
            volume.Add(new Simulation.Unit(player, print, new Vector3(8, 0, -8), Quaternion.Identity));


            var window = platform.CreateWindow();
            var screenSize = await window.GetSize();

            var uiState = new UI.Scenario(screenSize, volume);
            uiState.CurrentCamera.Position = new Vector3(0, 30, 0);
            uiState.CurrentCamera.Orientation = Quaternion.CreateFromAxisAngle(new Vector3(1, 0, 0), 90f.ToRadians());

            var controlScheme = new StandardControlScheme(uiState);
            var listener = new WindowListener(controlScheme);
            window.Listener = listener;

            using (var renderer = await platform.CreateRenderer(window))
            using (var cts = new CancellationTokenSource())
            {
                var renderLoop = CoreLoop(renderer, listener, scenario, uiState, controlScheme, cts.Token);

                await window.Closed;
                cts.Cancel();
                await renderLoop;
            }
        }

        static async Task CoreLoop(IRenderer renderer, WindowListener listener, Simulation.Scenario sim, UI.Scenario ui, IControlScheme controlScheme, CancellationToken token)
        {
            var frameCount = 0;
            var simWatch = new Watch();

            var renderWatch = new Stopwatch();
            renderWatch.Start();

            TimeSpan? previousFrame = null;
            var frameDelta = new List<TimeSpan>();

            using (var uiRenderer = new ScenarioRenderer())
            {
                while (!token.IsCancellationRequested)
                {
                    if (listener.Resize.TryConsume(out var resize))
                    {
                        renderer.Resize(resize.Value);
                        ui.ScreenSize = resize.Value;
                    }

                    controlScheme.Apply();

                    var renderTask = renderer.Render(ui.CurrentCamera, ui.CurrentVolume, draw => uiRenderer.Render(ui, draw));
                    await sim.Update(simWatch.MarkTime());
                    await renderTask;

                    if (previousFrame == null)
                    {
                        previousFrame = renderWatch.Elapsed;
                        frameDelta.Add(previousFrame.Value);
                    }
                    else
                    {
                        var currentFrame = renderWatch.Elapsed;
                        frameDelta.Add(currentFrame - previousFrame.Value);
                        previousFrame = currentFrame;
                    }

                    frameCount = frameCount + 1;
                }
            }

            Debug.Write($"FPS: {frameCount / renderWatch.Elapsed.TotalSeconds}\n");
            Debug.Write($"UPS: {sim.Rate.Ticks / renderWatch.Elapsed.TotalSeconds}\n");
        }
    }
}
