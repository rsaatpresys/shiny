using System;
using System.IO;


namespace Shiny.Web.Infrastructure
{
    public class WasmPlatform : IPlatform
    {
        public string Name => "WebAssembly";

        public PlatformState Status => throw new NotImplementedException();

        public DirectoryInfo AppData => throw new NotImplementedException();

        public DirectoryInfo Cache => throw new NotImplementedException();

        public DirectoryInfo Public => throw new NotImplementedException();

        public string AppIdentifier => throw new NotImplementedException();

        public string AppVersion => throw new NotImplementedException();

        public string AppBuild => throw new NotImplementedException();

        public string MachineName => throw new NotImplementedException();

        public string OperatingSystem => throw new NotImplementedException();

        public string OperatingSystemVersion => throw new NotImplementedException();

        public string Manufacturer => throw new NotImplementedException();

        public string Model => throw new NotImplementedException();

        public void InvokeOnMainThread(Action action)
        {
            throw new NotImplementedException();
        }

        public IObservable<PlatformState> WhenStateChanged()
        {
            throw new NotImplementedException();
        }
    }
}
