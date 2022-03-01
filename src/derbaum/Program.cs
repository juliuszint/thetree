
// This code was written for the OpenTK library and has been released
// to the Public Domain.
// It is provided "as is" without express or implied warranty of any kind.

// draw a coin

using System;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace derbaum
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            var gameWindowSettings = new GameWindowSettings();
            gameWindowSettings.UpdateFrequency = 60;
            gameWindowSettings.RenderFrequency = 60;


            /*
            : base(
                      new GraphicsMode(),
                      GameWindowFlags.Default,
                      DisplayDevice.Default,)
            */

            var nativeWindowSettings = new NativeWindowSettings();
            nativeWindowSettings.Title = "Der Baum";
            nativeWindowSettings.Size = new Vector2i(1280, 720);
            nativeWindowSettings.Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug;
            nativeWindowSettings.APIVersion = new Version(3, 2);

            using (DerBaumGameWindow example = new DerBaumGameWindow(gameWindowSettings, nativeWindowSettings))
            {
                // Get the title and category  of this example using reflection.
                //ExampleAttribute info = ((ExampleAttribute)example.GetType().GetCustomAttributes(false)[0]);
                //example.Title = String.Format("OpenTK | {0} {1}: {2}", info.Category, info.Difficulty, info.Title);
                example.Run();
            }
        }
    }
}
