using System;

namespace TrackingCamera
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TrackingCam game = new TrackingCam())
            {
                game.Run();
            }
        }
    }
#endif
}

