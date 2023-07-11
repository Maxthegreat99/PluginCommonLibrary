using System;
using System.Diagnostics;
using System.Linq;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace Terraria.Plugins.Common {
  public delegate void QueueExceptionHandler(Task task, object taskState, Exception exception);

  /// <summary>
  ///   Performs asynchronous tasks in a sequential order.
  /// </summary>
  public class AsyncWorkQueue: IDisposable {
    #region [Nested: WorkItem]
    private struct WorkItem {
      public static readonly WorkItem Invalid = default(WorkItem);

      public Type ResultType { get; private set; }
      public dynamic CompletionSource { get; private set; }
      public CancellationToken? CancellationToken { get; private set; }
      public dynamic Function { get; private set; }
      public dynamic State { get; private set; }
      public QueueExceptionHandler ExceptionHandler { get; private set; }


      public WorkItem(
        dynamic completionSource, CancellationToken? cancellationToken, dynamic function, dynamic state = null,
        QueueExceptionHandler exceptionHandler = null
      ): this() {

        this.CompletionSource = completionSource;
        this.CancellationToken = cancellationToken;
        this.Function = function;
        this.ResultType = null;
        this.State = state;
        this.ExceptionHandler = exceptionHandler;
      }
    }
    #endregion

    private readonly Thread[] workers;
    private readonly int workerTimeoutMs;
    private readonly CancellationTokenSource workerTokenSource;
    private readonly BlockingCollection<WorkItem> queuedItems;
    private readonly QueueExceptionHandler defaultExceptionHandler;

    public int WorkerThreadCount => this.workers.Length;


    public AsyncWorkQueue(
      string threadName = "Async Work Queue Thread", ThreadPriority threadPriority = ThreadPriority.BelowNormal,
      int workerThreadCount = 1, QueueExceptionHandler defaultExceptionHandler = null
    ) {
      this.workerTimeoutMs = -1;
      this.workerTokenSource = new CancellationTokenSource();
      this.queuedItems = new BlockingCollection<WorkItem>();
      this.workers = new Thread[workerThreadCount];
      this.defaultExceptionHandler = defaultExceptionHandler;

      for (int i = 0; i < workerThreadCount; i++) {
        Thread worker = new Thread(this.ProcessWorkItems) { IsBackground = false };
        this.workers[i] = worker;

        worker.Name = threadName;
        worker.Priority = threadPriority;
        worker.Start();
      }
    }

    public Task EnqueueTask(Action action, CancellationToken? cancellationToken = null, 
      QueueExceptionHandler exceptionHandler = null
    ) {
      if (this.isDisposed) throw new ObjectDisposedException(this.ToString());

      var completionSource = new TaskCompletionSource<object>();
      this.queuedItems.Add(new WorkItem(completionSource, cancellationToken, action, null, exceptionHandler));
      
      return completionSource.Task;
    }

    public Task EnqueueTask<TState>(
      Action<TState> action, TState state, CancellationToken? cancellationToken = null,
      QueueExceptionHandler exceptionHandler = null
    ) {
      if (this.isDisposed) throw new ObjectDisposedException(this.ToString());

      var completionSource = new TaskCompletionSource<object>();
      this.queuedItems.Add(new WorkItem(completionSource, cancellationToken, action, state, exceptionHandler));
      
      return completionSource.Task;
    }

    public Task<TResult> EnqueueTask<TResult>(
      Func<TResult> function, CancellationToken? cancellationToken = null, QueueExceptionHandler exceptionHandler = null
    ) {
      if (this.isDisposed) throw new ObjectDisposedException(this.ToString());

      var completionSource = new TaskCompletionSource<TResult>();
      this.queuedItems.Add(new WorkItem(completionSource, cancellationToken, function, null, exceptionHandler));
      
      return completionSource.Task;
    }

    public Task<TResult> EnqueueTask<TState,TResult>(
      Func<TState,TResult> function, TState state, CancellationToken? cancellationToken = null,
      QueueExceptionHandler exceptionHandler = null
    ) {
      if (this.isDisposed) throw new ObjectDisposedException(this.ToString());

      var completionSource = new TaskCompletionSource<TResult>();
      this.queuedItems.Add(new WorkItem(completionSource, cancellationToken, function, state, exceptionHandler));
      
      return completionSource.Task;
    }

    public void AwaitAll(int timeoutMs = -1) {
      if (this.isDisposed) throw new ObjectDisposedException(this.ToString());

      this.AwaitAllInternal(timeoutMs);
    }

    private void AwaitAllInternal(int timeoutMs) {
      Task[] itemTasks = new Task[this.queuedItems.Count];
      int counter = 0;
      foreach (Task<object> itemTask in this.queuedItems.Select(item => item.CompletionSource.Task))
        itemTasks[counter++] = itemTask;

      Task.WaitAll(itemTasks, timeoutMs);
    }

    private void ProcessWorkItems() {
      try {
        WorkItem currentItem;
        while (this.queuedItems.TryTake(out currentItem, this.workerTimeoutMs, this.workerTokenSource.Token)) {
          if (currentItem.Equals(WorkItem.Invalid))
            throw new InvalidOperationException("Can not process an invalid work item.");

          if (currentItem.CancellationToken.HasValue && currentItem.CancellationToken.Value.IsCancellationRequested) {
            currentItem.CompletionSource.SetCanceled();
          } else {
            try {
              dynamic result = null;
              if (currentItem.Function is Action)
                currentItem.Function();
              else if (currentItem.Function.GetType().GetGenericTypeDefinition().FullName.StartsWith("System.Func`1"))
                result = currentItem.Function();
              else if (currentItem.Function.GetType().GetGenericTypeDefinition().FullName.StartsWith("System.Action`1"))
                currentItem.Function(currentItem.State);
              else
                result = currentItem.Function(currentItem.State);

              // Note: This will also execute all continuing tasks.
              currentItem.CompletionSource.SetResult(result);
            } catch (OperationCanceledException ex) {
              if (ex.CancellationToken == currentItem.CancellationToken)
                currentItem.CompletionSource.SetCanceled();
              else
                currentItem.CompletionSource.SetException(ex);
            } catch (Exception ex) {
              currentItem.CompletionSource.SetException(ex);

              if (currentItem.ExceptionHandler != null) {
                try {
                  currentItem.ExceptionHandler(currentItem.CompletionSource.Task, currentItem.State, ex);
                } catch (Exception exInner) {
                  Debug.WriteLine("An exception handler of an enqueued work item has thrown an exception: \n" + exInner);
                }
              }
            }
          }
        }
      } catch (OperationCanceledException) {
        Debug.WriteLine("workerTokenSource.Token was set. Shutting down queue...");
      } catch (ThreadAbortException) {
        Debug.WriteLine("Thread of async work queue was aborted. Shutting down queue...");
      } catch (Exception ex) {
        Debug.WriteLine("{0} has thrown an unexpected exception: \n{1}", Thread.CurrentThread.Name, ex);
      }
    }

    #region [IDisposable Implementation]
    private bool isDisposed;
    public bool IsDisposed => this.isDisposed;

    protected virtual void Dispose(bool isDisposing) {
      if (this.isDisposed)
        return;
    
      this.isDisposed = true;
      if (isDisposing) {
        if (this.workerTokenSource != null) {
          this.workerTokenSource.Cancel();

          for (int i = 0; i < this.workers.Length; i++) {
            try {
              this.workers[i].Join(TimeSpan.FromSeconds(30));
            } catch (AggregateException) {
              this.workers[i].Interrupt();
            }
          }
        }

        this.workerTokenSource?.Dispose();
      }
    }

    public void Dispose() {
      queuedItems.Dispose();
      this.Dispose(true);
      GC.SuppressFinalize(this);
    }

    ~AsyncWorkQueue() {
      this.Dispose(false);
    }
    #endregion
  }
}
