using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using SolidWorks.Interop.sldworks;
using SolidWorks.Interop.swpublished;

namespace GeminiSwAddin
{
    [ComVisible(true)]
    [Guid("9C1B2F8D-7E3F-4A5B-BF2E-3F5D8C1A9B2F")]
    [ProgId("GeminiSwAddin.SwAddin")]
    [ClassInterface(ClassInterfaceType.None)]
    [SwAddin(Description = "Gemini Task Pane Addin", Title = "Gemini")]
    public class SwAddin : ISwAddin
    {
        private SldWorks _swApp;
        private int _addinCookie;
        private TaskpaneView _taskPaneView;
        private GeminiTaskPaneControl _taskPaneControl;

        public bool ConnectToSW(object ThisSW, int Cookie)
        {
            try
            {
                _swApp = (SldWorks)ThisSW;
                _addinCookie = Cookie;

                GeminiTaskPaneControl.CurrentSwApp = _swApp;

                string solidWorksRoot = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
                string iconPath = Path.Combine(solidWorksRoot, "data", "graphics", "Images", "preview", "color_base_dm_32.bmp");
                _taskPaneView = _swApp.CreateTaskpaneView2(iconPath, "Gemini");
                _taskPaneView.AddControl("GeminiSwAddin.GeminiTaskPaneControl", "");

                return true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to connect add-in: " + ex.Message, "Gemini Addin", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        public bool DisconnectFromSW()
        {
            try
            {
                if (_taskPaneView != null)
                {
                    try
                    {
                        dynamic tp = _taskPaneView;
                        tp.RemoveControl("Gemini");
                    }
                    catch { }

                    _taskPaneView = null;
                }

                _taskPaneControl?.Dispose();
                _taskPaneControl = null;
                GeminiTaskPaneControl.CurrentSwApp = null;

                _swApp = null;
                return true;
            }
            catch
            {
                return false;
            }
        }

        [ComRegisterFunction]
        public static void RegisterFunction(Type type)
        {
            using (RegistryKey addinKey = Registry.LocalMachine.CreateSubKey(
                @"Software\SolidWorks\Addins\{" + type.GUID.ToString().ToUpperInvariant() + "}"))
            {
                addinKey.SetValue(null, 0, RegistryValueKind.DWord);
                addinKey.SetValue("Title", "Gemini");
                addinKey.SetValue("Description", "Google Gemini in a SolidWorks Task Pane");
            }

            using (RegistryKey startupKey = Registry.CurrentUser.CreateSubKey(
                @"Software\SolidWorks\AddInsStartup\{" + type.GUID.ToString().ToUpperInvariant() + "}"))
            {
                startupKey.SetValue(null, 1, RegistryValueKind.DWord);
            }
        }

        [ComUnregisterFunction]
        public static void UnregisterFunction(Type type)
        {
            string guid = "{" + type.GUID.ToString().ToUpperInvariant() + "}";
            Registry.LocalMachine.DeleteSubKeyTree(@"Software\SolidWorks\Addins\" + guid, false);
            Registry.CurrentUser.DeleteSubKeyTree(@"Software\SolidWorks\AddInsStartup\" + guid, false);
        }
    }
}
