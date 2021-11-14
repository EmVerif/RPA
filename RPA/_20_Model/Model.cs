using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using ICSharpCode.AvalonEdit.Document;

using Microsoft.CodeAnalysis.CSharp.Scripting;
using Microsoft.CodeAnalysis.Scripting;

using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace RPA._20_Model
{
    class Model : INotifyPropertyChanged
    {
        public static Model Instance = new Model();
        public event PropertyChangedEventHandler PropertyChanged;

        public TextDocument Script { get; } = new TextDocument();
        public Boolean IsReadOnly { get; set; } = false;
        public bool IsScriptRunning
        {
            get
            {
                return (_CaptureAndRecognizeTask != null);
            }
        }
        private string _Log = "";
        public string Log
        {
            get
            {
                return _Log;
            }
            private set
            {
                _Log = value;
                OnPropertyChanged(nameof(this.Log));
            }
        }
        private object _RecognizedResultListLock = new object();
        private List<RecognizedResult> _RecognizedResultList = null;
        public List<RecognizedResult> RecognizedResultList
        {
            get
            {
                List<RecognizedResult> ret;

                lock (_RecognizedResultListLock)
                {
                    ret = _RecognizedResultList;
                    _RecognizedResultList = null;
                }

                return ret;
            }
        }

        private CancellationTokenSource _CaptureAndRecognizeCancelTaken;
        private Task _CaptureAndRecognizeTask = null;
        private Task _ScriptTask = null;
        private double _WidthThresh = 10.0;

        public void StartStopScript()
        {
            if (_CaptureAndRecognizeTask == null)
            {
                StartScript();
            }
            else
            {
                StopScript();
            }
        }

        public void SetLog(string log)
        {
            Log += log;
        }

        private async void StartScript()
        {
            string scriptText = Script.Text;

            Script.Text = "";
            IsReadOnly = true;
            OnPropertyChanged(nameof(this.Script));
            OnPropertyChanged(nameof(this.IsReadOnly));
            Log = "";
            _CaptureAndRecognizeCancelTaken = new CancellationTokenSource();
            var token = _CaptureAndRecognizeCancelTaken.Token;
            _CaptureAndRecognizeTask = Task.Run(() =>
            {
                while (!token.IsCancellationRequested)
                {
                    CaptureAndRecognizeAsync().Wait();
                    try
                    {
                        Task.Delay(500, token).Wait();
                    }
                    catch
                    {
                    }
                }
            }
            );
            OnPropertyChanged(nameof(this.IsScriptRunning));
            ScriptOptions options = ScriptOptions.Default.WithImports("System", "System.Collections.Generic");
            Script<object> script = CSharpScript.Create(scriptText, options, typeof(PublicApis));
            _ScriptTask = Task.Run(() =>
            {
                try
                {
                    script.RunAsync(new PublicApis(token)).Wait();
                }
                catch (Exception ex)
                {
                    if (ex.InnerException != null)
                    {
                        MessageBox.Show(ex.InnerException.Message);
                    }
                    else
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            });
            await _ScriptTask;
            Script.Text = scriptText;
            IsReadOnly = false;
            OnPropertyChanged(nameof(this.Script));
            OnPropertyChanged(nameof(this.IsReadOnly));
            StopScript();
        }

        private void StopScript()
        {
            if (_CaptureAndRecognizeTask != null)
            {
                _CaptureAndRecognizeCancelTaken.Cancel();
                _ScriptTask.Wait();
                _CaptureAndRecognizeTask.Wait();
                _ScriptTask.Dispose();
                _CaptureAndRecognizeTask.Dispose();
                _CaptureAndRecognizeTask = null;
                OnPropertyChanged(nameof(this.IsScriptRunning));
            }
        }

        private async Task CaptureAndRecognizeAsync()
        {
            using (Bitmap capturedBitmap = Capture())
            {
                var softwareBitmap = await _90_Common.Common.ToSoftwareBitmap(capturedBitmap);
                await Recognize(softwareBitmap);
            }
        }

        private Bitmap Capture()
        {
            Bitmap ret = new Bitmap(
                (int)SystemParameters.VirtualScreenWidth,
                (int)SystemParameters.VirtualScreenHeight,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            using (var bmpGraphics = Graphics.FromImage(ret))
            {
                bmpGraphics.CopyFromScreen(
                    (int)SystemParameters.VirtualScreenLeft,
                    (int)SystemParameters.VirtualScreenTop,
                    0, 0, ret.Size);
            }

            return ret;
        }

        private async Task Recognize(SoftwareBitmap softwareBitmap)
        {
            OcrEngine engine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("ja-JP"));
            var result = await engine.RecognizeAsync(softwareBitmap);
            var recognizedResultList = new List<RecognizedResult>();
            double offsetX = SystemParameters.VirtualScreenLeft;
            double offsetY = SystemParameters.VirtualScreenTop;

            foreach (var ocrLine in result.Lines)
            {
                double prevRight = double.MinValue;
                double right = double.MinValue;
                double left = double.MinValue;
                double top = double.MaxValue;
                double bottom = double.MinValue;
                string concatenateResult = "";

                foreach (var ocrWord in ocrLine.Words)
                {
                    double diffX = ocrWord.BoundingRect.Left - prevRight;
                    if (diffX > _WidthThresh)
                    {
                        if (concatenateResult.Length != 0)
                        {
                            var recognizedResult = new RecognizedResult();

                            recognizedResult.Text = concatenateResult;
                            recognizedResult.Right = ocrWord.BoundingRect.Right + offsetX;
                            recognizedResult.Left = left + offsetX;
                            recognizedResult.Top = top + offsetY;
                            recognizedResult.Bottom = bottom + offsetY;
                            recognizedResultList.Add(recognizedResult);
                        }
                        right = ocrWord.BoundingRect.Right;
                        left = ocrWord.BoundingRect.Left;
                        top = ocrWord.BoundingRect.Top;
                        bottom = ocrWord.BoundingRect.Bottom;
                        concatenateResult = ocrWord.Text;
                    }
                    else
                    {
                        right = ocrWord.BoundingRect.Right;
                        top = Math.Min(top, ocrWord.BoundingRect.Top);
                        bottom = Math.Max(bottom, ocrWord.BoundingRect.Bottom);
                        concatenateResult += ocrWord.Text;
                    }
                    prevRight = ocrWord.BoundingRect.Right;
                }
                if (concatenateResult.Length != 0)
                {
                    var recognizedResult = new RecognizedResult();

                    recognizedResult.Text = concatenateResult;
                    recognizedResult.Right = right + offsetX;
                    recognizedResult.Left = left + offsetX;
                    recognizedResult.Top = top + offsetY;
                    recognizedResult.Bottom = bottom + offsetY;
                    recognizedResultList.Add(recognizedResult);
                }
            }
            lock (_RecognizedResultListLock)
            {
                _RecognizedResultList = recognizedResultList;
            }
        }

        private void OnPropertyChanged(string info)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(info));
        }
    }

    public class RecognizedResult
    {
        public string Text;
        public double Right;
        public double Left;
        public double Top;
        public double Bottom;
        public double CenterX
        {
            get
            {
                return ((Left + Right) / 2);
            }
        }
        public double CenterY
        {
            get
            {
                return ((Top + Bottom) / 2);
            }
        }
    }
}
