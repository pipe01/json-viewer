using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace JSON_Viewer
{
    public static class Extensions
    {
        public static void InvokeDelayed(this Dispatcher dispatcher, int delayMs, Action action)
        {
            Task.Delay(delayMs).ContinueWith(_ => dispatcher.Invoke(action));
        }
    }
}
