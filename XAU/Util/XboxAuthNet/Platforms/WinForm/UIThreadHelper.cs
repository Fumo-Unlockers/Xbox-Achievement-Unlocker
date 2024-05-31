using System;
using System.Threading;
using System.Threading.Tasks;

namespace XboxAuthNet.Platforms.WinForm
{
    internal static class UIThreadHelper
    {
        public static async Task InvokeUIActionOnSafeThread(
            Action action, 
            SynchronizationContext? synchronizationContext, 
            CancellationToken cancellationToken)
        {
            if (Thread.CurrentThread.GetApartmentState() == ApartmentState.MTA)
            {
                await invokeWithinMtaThread(action, synchronizationContext, cancellationToken);
            }
            else
            {
                action.Invoke();
            }
        }

        private static async Task invokeWithinMtaThread(
            Action action, 
            SynchronizationContext? synchronizationContext,
            CancellationToken cancellationToken)
        {
            if (synchronizationContext != null)
            {
                var actionWithTcs = new Action<object?>((tcs) =>
                {
                    try
                    {
                        action.Invoke();
                        ((TaskCompletionSource<object?>)tcs!).TrySetResult(null);
                    }
                    catch (Exception e)
                    {
                        // Need to catch the exception here and put on the TCS which is the task we are waiting on so that
                        // the exception comming out of Authenticate is correctly thrown.
                        ((TaskCompletionSource<object>)tcs!).TrySetException(e);
                    }
                });

                var tcs2 = new TaskCompletionSource<object?>();

                synchronizationContext.Post(
                    new SendOrPostCallback(actionWithTcs), tcs2);
                await tcs2.Task.ConfigureAwait(false);
            }
            else
            {
                using (var staTaskScheduler = new StaTaskScheduler(1))
                {
                    try
                    {
                        Task.Factory.StartNew(
                            action,
                            cancellationToken,
                            TaskCreationOptions.None,
                            staTaskScheduler).Wait();
                    }
                    catch (AggregateException ae)
                    {
                        // Any exception thrown as a result of running task will cause AggregateException to be thrown with
                        // actual exception as inner.
                        Exception innerException = ae.InnerExceptions[0];

                        // In MTA case, AggregateException is two layer deep, so checking the InnerException for that.
                        if (innerException is AggregateException)
                        {
                            innerException = ((AggregateException)innerException).InnerExceptions[0];
                        }

                        throw innerException;
                    }
                }
            }
        }
    }
}