using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using RPA._90_Common;

namespace RPA._20_Model
{
    public class PublicApis
    {
        public class Result
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
        private CancellationToken _Token;

        public PublicApis(CancellationToken token)
        {
            this._Token = token;
        }

        public List<Result> GetCurrentOcrResult()
        {
            CheckForceTerm();
            List<Result> ret = new List<Result>();
            List<RecognizedResult> allResultList;
            do
            {
                Task.Delay(100).Wait();
                allResultList = Model.Instance.RecognizedResultList;
            }
            while (
                allResultList == null &&
                !_Token.IsCancellationRequested
            );
            foreach (var x in allResultList)
            {
                var result = new Result();
                result.Text = x.Text;
                result.Left = x.Left;
                result.Right = x.Right;
                result.Top = x.Top;
                result.Bottom = x.Bottom;
                ret.Add(result);
            }

            return ret;
        }

        public List<Result> ScreenStringSearch(string SearchString, double TimeoutSec = 10.0)
        {
            CheckForceTerm();
            List<Result> ret = new List<Result>();
            Stopwatch stopWatch = new Stopwatch();

            stopWatch.Start();
            while (
                !_Token.IsCancellationRequested &&
                (stopWatch.Elapsed.TotalMilliseconds < (int)(TimeoutSec * 1000))
            )
            {
                List<RecognizedResult> allResultList = Model.Instance.RecognizedResultList;
                if (allResultList != null)
                {
                    var x = allResultList.FindAll(x => x.Text.Contains(SearchString));
                    if (x.Count != 0)
                    {
                        foreach (var y in x)
                        {
                            var result = new Result();
                            result.Text = y.Text;
                            result.Left = y.Left;
                            result.Right = y.Right;
                            result.Top = y.Top;
                            result.Bottom = y.Bottom;
                            ret.Add(result);
                        }
                        break;
                    }
                }
                Task.Delay(100).Wait();
            }
            stopWatch.Stop();

            return ret;
        }

        public void Sleep(int MillisecTime)
        {
            try
            {
                Task.Delay(MillisecTime, _Token).Wait();
            }
            catch
            {
                CheckForceTerm();
            }
        }

        public void LogOut(string Log)
        {
            CheckForceTerm();
            Model.Instance.SetLog(Log);
        }

        public void GetMousePos(out double X, out double Y)
        {
            CheckForceTerm();
            Win32Point win32Point = new Win32Point();
            KeyboardMouseCtrl.GetCursorPos(ref win32Point);
            X = win32Point.X;
            Y = win32Point.Y;
        }

        public void MouseSetPosition(double X, double Y)
        {
            CheckForceTerm();
            KeyboardMouseCtrl.SetCursorPos((int)X, (int)Y);
        }

        public void MouseClick(double X, double Y)
        {
            Input[] inputs = new Input[] {
                new Input {
                    type = KeyboardMouseCtrl.INPUT_MOUSE,
                    ui = new INPUT_UNION {
                        mouse = new MOUSEINPUT {
                            dwFlags = KeyboardMouseCtrl.MOUSEEVENTF_LEFTDOWN,
                            dx = 0,
                            dy = 0,
                            mouseData = 0,
                            dwExtraInfo = IntPtr.Zero,
                            time = 0
                        }
                    }
                },
                new Input {
                    type = KeyboardMouseCtrl.INPUT_MOUSE,
                    ui = new INPUT_UNION {
                        mouse = new MOUSEINPUT {
                            dwFlags = KeyboardMouseCtrl.MOUSEEVENTF_LEFTUP,
                            dx = 0,
                            dy = 0,
                            mouseData = 0,
                            dwExtraInfo = IntPtr.Zero,
                            time = 0
                        }
                    }
                }
            };
            KeyboardMouseCtrl.SetCursorPos((int)X, (int)Y);
            KeyboardMouseCtrl.SendInput(1, ref inputs[0], Marshal.SizeOf(inputs[0]));
            this.Sleep(30);
            KeyboardMouseCtrl.SendInput(1, ref inputs[1], Marshal.SizeOf(inputs[1]));
            this.Sleep(30);
        }

        public void KeyInput(string String)
        {
            CheckForceTerm();
            System.Windows.Forms.SendKeys.SendWait(String);
        }

        public void AppBoot(string Path, string Args = null)
        {
            CheckForceTerm();
            ProcessStartInfo pInfo = new ProcessStartInfo();
            pInfo.FileName = Path;
            if (Args != null)
            {
                pInfo.Arguments = Args;
            }

            Process.Start(pInfo);
        }

        private void CheckForceTerm()
        {
            if (_Token.IsCancellationRequested)
            {
                throw new Exception("強制終了");
            }
        }
    }
}
