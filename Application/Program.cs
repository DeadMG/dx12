using Data.Mesh;
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
            var directory = Directory.GetCurrentDirectory();
            var platform = new PlatformSelector().GetPlatform();

            await platform.OneTimeInitialisation();

            var scenario = new Simulation.Scenario();
            var player = scenario.AddPlayer(scenario.AddForce());

            var print = new Blueprint
            {
                Name = "Fighter",
                Mesh = Mesh.NewFromPoints(
                    "Hypercraft",
                    [
                        new Vector3(3.0f, 0.0f, 0.0f),
                        new Vector3(0.0f, 3.0f, -3.0f),
                        new Vector3(0.0f, 0.0f, 10.0f),
                        new Vector3(-3.0f, 0.0f, 0.0f),
                        
                        // left gun
                        new Vector3(3.2f, -1.0f, -3.0f),
                        new Vector3(3.2f, -1.0f, 11.0f),
                        new Vector3(2.0f, 1.0f, 2.0f),
                        
                        // right gun
                        new Vector3(-3.2f, -1.0f, -3.0f),
                        new Vector3(-3.2f, -1.0f, 11.0f),
                        new Vector3(-2.0f, 1.0f, 2.0f)
                    ],
                    [
                        new Triangle { Vertices = [0, 1, 2], MaterialIndex = 1 },
                        new Triangle { Vertices = [2, 1, 3], MaterialIndex = 2 },
                        new Triangle { Vertices = [3, 1, 0], MaterialIndex = 1 },
                        new Triangle { Vertices = [0, 2, 3], MaterialIndex = 2 },
                        new Triangle { Vertices = [6, 5, 4], MaterialIndex = 3 },
                        new Triangle { Vertices = [7, 8, 9], MaterialIndex = 4 },
                    ], 
                    [
                        new Material
                        {
                            EmissionStrength = 0,
                            EmissionColour = new RGB(0, 0, 0),
                            Colour = new RGB(0.1f, 0.1f, 1)
                        },
                        new Material
                        {
                            EmissionStrength = 0,
                            EmissionColour = new RGB(0, 0, 0),
                            Colour = new RGB(0, 1, 0)
                        },
                        new Material
                        {
                            EmissionStrength = 0,
                            EmissionColour = new RGB(0, 0, 0),
                            Colour = new RGB(1, 0, 0)
                        },
                        new Material
                        {
                            EmissionStrength = 1f,
                            EmissionColour = new RGB(1, 0.5f, 0),
                            Colour = new RGB(1, 1, 1)
                        },
                        new Material
                        {
                            EmissionStrength = 1f,
                            EmissionColour = new RGB(0, 0.5f, 1),
                            Colour = new RGB(1, 1, 1)
                        },
                    ]
                ),
                Acceleration = 3,
                MaxSpeed = 100,
                TurnRate = (float)Math.PI / 2,
            };

            var map = new Map {
                AmbientLightLevel = 0.1f,
                Dimensions = new Vector3(100000, 100000, 100000),
                Objects = [
                    new PredefinedObject
                    {
                        Geometry = new SphereGeometry 
                        {
                            Material = new Material { EmissionStrength = 1f, EmissionColour = RGB.From255(242, 241, 247), Colour = RGB.From255(153, 170, 240) } 
                        },
                        WorldMatrix = Matrix4x4.CreateScale(20) * Matrix4x4.CreateTranslation(new Vector3(-40, 0, 40)),
                        Name = "Blue sun",
                    }
                ],
                StarfieldNoiseCutoff = 0.95f,
                StarfieldNoiseScale = 500f,
                StarfieldTemperatureScale = 600f,
                StarfieldSeed = null,
                StarCategories = [
                    new StarCategory { Cutoff = 0.1f, Colour = RGB.From255(153, 170, 240) },
                    new StarCategory { Cutoff = 0.2f, Colour = RGB.From255(156, 173, 224) },
                    new StarCategory { Cutoff = 0.3f, Colour = RGB.From255(171, 179, 209) },
                    new StarCategory { Cutoff = 0.5f, Colour = RGB.From255(242, 241, 247) },
                    new StarCategory { Cutoff = 0.6f, Colour = RGB.From255(230, 100, 100) },
                    new StarCategory { Cutoff = 0.7f, Colour = RGB.From255(220, 50, 50) },
                    new StarCategory { Cutoff = 0.8f, Colour = RGB.From255(245, 237, 231) },
                    new StarCategory { Cutoff = 1f, Colour = RGB.From255(237, 200, 159) },
                ],
                Id = Guid.NewGuid(),
                Name = "Test",
            };
            var volume = scenario.AddVolume(map);

            volume.Add(new Unit(player, print, new Vector3(8, 0, 8), Quaternion.Identity));
            volume.Add(new Unit(player, print, new Vector3(-8, 0, 8), Quaternion.Identity));
            volume.Add(new Unit(player, print, new Vector3(-8, 0, -8), Quaternion.Identity));
            volume.Add(new Unit(player, print, new Vector3(8, 0, -8), Quaternion.Identity));

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

                    var renderTask = renderer.Render(new VolumeRenderTask { Camera = ui.CurrentCamera, Volume = ui.CurrentVolume }, draw => uiRenderer.Render(ui, draw));
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
