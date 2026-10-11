using System;
using System.Runtime.InteropServices;

namespace SolidWorks.Interop.sldworks
{
    // Minimal stubs to allow compilation in development environments without SolidWorks interop assemblies.
    [ComVisible(true)]
    public interface ISwAddin
    {
        bool ConnectToSW(object ThisSW, int Cookie);
        bool DisconnectFromSW();
    }

    [ComVisible(true)]
    public class SldWorks
    {
        // ActiveDoc will be replaced at runtime by real SolidWorks object when running inside SolidWorks.
        public object ActiveDoc { get; set; }

        public object CreateTaskpaneView3(string title, object options) => null;
        public object CreateTaskpaneView2() => null;
        public object CreateTaskpaneView() => null;
    }

    [ComVisible(true)]
    public class TaskpaneView
    {
        public void AddControl(object control, string caption) { }
        public void AddControl2(object control, string caption) { }
        public object Control { get; set; }
        public void RemoveControl(string caption) { }
    }

    [ComVisible(true)]
    public class ModelDoc2
    {
        // Simplified SaveBMP signature used in sample code.
        public bool SaveBMP(string fileName, int width, int height) => false;
    }

    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class SwAddinAttribute : Attribute
    {
        public string Description { get; set; }
        public string Title { get; set; }

        public SwAddinAttribute() { }
    }
}

namespace SolidWorks.Interop.swconst
{
    // Placeholder for swconst namespace to satisfy references.
    public static class swDocumentTypes_e { }
}
