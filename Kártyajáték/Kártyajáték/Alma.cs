using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;

namespace OminousMenu
{
    public static class SbExt
    {
        public static Task BeginAsync(this Storyboard sb, FrameworkElement el)
        {
            var tcs = new TaskCompletionSource<bool>();
            void OnCompleted(object s, EventArgs e)
            {
                sb.Completed -= OnCompleted;
                tcs.TrySetResult(true);
            }
            sb.Completed += OnCompleted;
            sb.Begin(el);
            return tcs.Task;
        }
    }
}