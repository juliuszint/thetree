
// This code was written for the OpenTK library and has been released
// to the Public Domain.
// It is provided "as is" without express or implied warranty of any kind.

// draw a coin

using System;

namespace derbaum
{
    public static class Program
    {
        [STAThread]
        public static void Main()
        {
            using (DerBaumGameWindow example = new DerBaumGameWindow())
            {
                // Get the title and category  of this example using reflection.
                //ExampleAttribute info = ((ExampleAttribute)example.GetType().GetCustomAttributes(false)[0]);
                //example.Title = String.Format("OpenTK | {0} {1}: {2}", info.Category, info.Difficulty, info.Title);
                example.Run(30.0, 0.0);
            }
        }
    }

}
