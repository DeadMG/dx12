using Platform.Contracts;
using Platform.Windows;
using System.Runtime.InteropServices;

namespace Application
{
    public class PlatformSelector
    {
        public IPlatform GetPlatform()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return new WindowsPlatform();

            throw new InvalidOperationException("Unrecognized platform");
        }
    }
}
