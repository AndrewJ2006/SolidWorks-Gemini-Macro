using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using SolidWorks.Interop.sldworks;

namespace GeminiSwAddin
{
    [ComVisible(true)]
    [Guid("C2F64F11-9C19-4CF7-9B6A-4DAA0E5F6D13")]
    [ProgId("GeminiSwAddin.GeminiTaskPaneControl")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class GeminiTaskPaneControl : UserControl
    {
        public static SldWorks CurrentSwApp { get; set; }

        private readonly SldWorks _swApp;
        private readonly WebView2 _webView;
        private readonly string _userDataFolder;
        private bool _webViewReady;
        private bool _captureInProgress;

        public GeminiTaskPaneControl()
            : this(CurrentSwApp)
        {
        }

        public GeminiTaskPaneControl(SldWorks swApp)
        {
            _swApp = swApp ?? throw new ArgumentNullException(nameof(swApp));
            SuspendLayout();

            _userDataFolder = Path.Combine(
                System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData),
                "GeminiSwAddin",
                "WebView2UserData");

            _webView = new WebView2 { Dock = DockStyle.Fill };

            Controls.Add(_webView);
            Load += GeminiTaskPaneControl_Load;
            ResumeLayout();
        }

        private async void GeminiTaskPaneControl_Load(object sender, EventArgs e)
        {
            await InitializeWebView2Async();
        }

        private async Task InitializeWebView2Async()
        {
            try
            {
                var env = await CoreWebView2Environment.CreateAsync(userDataFolder: _userDataFolder);
                await _webView.EnsureCoreWebView2Async(env);

                _webView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
                _webView.CoreWebView2.Settings.IsStatusBarEnabled = false;
                _webView.CoreWebView2.Settings.AreDevToolsEnabled = false;
                _webView.CoreWebView2.NavigationCompleted += WebView_NavigationCompleted;
                _webView.CoreWebView2.WebMessageReceived += WebMessageReceived;
                _webView.CoreWebView2.Navigate("https://gemini.google.com");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to initialize WebView2: " + ex.Message,
                    "Gemini Addin", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void WebView_NavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            _webViewReady = e.IsSuccess;
            if (e.IsSuccess)
            {
                _ = InstallCaptureButtonAsync();
            }
        }

        private async void WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            if (e.TryGetWebMessageAsString() == "capture-viewport")
            {
                await CaptureAndPasteAsync();
            }
        }

        private async Task InstallCaptureButtonAsync()
        {
            if (_webView.CoreWebView2 == null)
            {
                return;
            }

            await _webView.CoreWebView2.ExecuteScriptAsync(@"(() => {
                const id = 'solidworks-capture-viewport';
                let button = document.getElementById(id);
                if (!button) {
                    button = document.createElement('button');
                    button.id = id;
                    button.type = 'button';
                    button.textContent = 'Capture View';
                    button.title = 'Capture the SolidWorks viewport and paste it into Gemini';
                    button.addEventListener('click', () => chrome.webview.postMessage('capture-viewport'));
                    document.body.appendChild(button);
                }
                button.style.cssText = [
                    'position: fixed',
                    'top: 8px',
                    'right: 76px',
                    'z-index: 2147483647',
                    'height: 26px',
                    'padding: 0 12px',
                    'border: 1px solid #5f6368',
                    'border-radius: 13px',
                    'background: #1f1f1f',
                    'color: #ffffff',
                    'font: 700 12px Arial, sans-serif',
                    'cursor: pointer',
                    'box-shadow: 0 1px 4px rgba(0,0,0,.45)'
                ].join(';');
            })();");
        }

        private async Task CaptureAndPasteAsync()
        {
            if (_captureInProgress || !_webViewReady)
            {
                if (!_webViewReady)
                {
                    MessageBox.Show("Wait for Gemini to finish loading before capturing.",
                        "Capture Not Ready", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                return;
            }

            _captureInProgress = true;
            string tempFile = null;

            try
            {
                var activeDoc = _swApp?.ActiveDoc as ModelDoc2;
                if (activeDoc == null)
                {
                    MessageBox.Show("No active model open in SolidWorks.",
                        "Capture Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                tempFile = Path.Combine(Path.GetTempPath(),
                    $"sw_viewport_{Guid.NewGuid():N}.bmp");

                bool saved = false;
                try
                {
                    saved = activeDoc.SaveBMP(tempFile, 0, 0);
                }
                catch
                {
                    try
                    {
                        object result = ((object)activeDoc).GetType().InvokeMember(
                            "SaveBMP",
                            System.Reflection.BindingFlags.InvokeMethod,
                            null,
                            activeDoc,
                            new object[] { tempFile, 0, 0 });
                        saved = result is bool value && value;
                    }
                    catch
                    {
                        saved = false;
                    }
                }

                if (!saved || !File.Exists(tempFile))
                {
                    MessageBox.Show("Failed to export viewport to BMP.",
                        "Capture Failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                using (var source = new Bitmap(tempFile))
                using (var clipboardImage = new Bitmap(source))
                {
                    Clipboard.Clear();
                    Clipboard.SetImage(clipboardImage);
                }

                _webView.Focus();
                await _webView.CoreWebView2.ExecuteScriptAsync(@"(() => {
                    const input = document.querySelector('textarea, [contenteditable=true], [role=textbox]');
                    if (input) { input.focus(); input.click(); return true; }
                    return false;
                })();");

                await Task.Delay(150);
                SendKeys.SendWait("^v");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Capture failed: " + ex.Message,
                    "Gemini Addin", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                if (!string.IsNullOrEmpty(tempFile))
                {
                    try { File.Delete(tempFile); } catch { }
                }

                _captureInProgress = false;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _webView?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
