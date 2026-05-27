using Go.Runtime.Collections;
using Go.Runtime.Common;
using System;
using System.Threading;

namespace Go.Runtime.Concurrency;


/// <summary>
/// 任务执行服务：底层的通用任务队列与线程调度中心
/// </summary>
public class WorkService
{
    public class WorkGuard : IDisposable
    {
        private WorkService? _service;

        internal WorkGuard(WorkService service)
        {
            _service = service;
            _service.HoldWork();
        }

        public void Dispose()
        {
            var service = Interlocked.Exchange(ref _service, null);
            if (service != null)
            {
                service.ReleaseWork();
            }
        }
    }

    private readonly MsgQueue<Action> _opQueue;
    private int _workCount;             // 活跃地工作标记数（WorkGuard机制）
    private int _waitingThreads;        // 阻塞等挂起等待地物理线程数
    private volatile bool _isRunning;   // 运行控制总开关

    public WorkService()
    {
        _workCount = 0;
        _waitingThreads = 0;
        _isRunning = true;
        _opQueue = new MsgQueue<Action>();
    }

    public void HoldWork() => Interlocked.Increment(ref _workCount);

    /// <summary>
    /// 投递任务（无阻塞）：外界任何线程都可以安全地往队列丢任务并触发唤醒
    /// </summary>
    /// <param name="handler"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void Post(Action handler)
    {
        var newNode = new MsgQueueNode<Action>(handler);

        Monitor.Enter(_opQueue);
        try
        {
            _opQueue.AddLast(newNode);

            if (_waitingThreads != 0)
            {
                _waitingThreads--;
                Monitor.Pulse(_opQueue);    // 【关键核心】：刺醒正在沉睡等待任务的物理线程！
            }
        }
        finally
        {
            Monitor.Exit(_opQueue);
        }
    }

    public void ReleaseWork()
    {
        // 如果外部保护归零，说明要停机了，唤醒所有还在沉睡的线程起来打卡下班（退出 Run）
        if (0 == Interlocked.Decrement(ref _workCount))
        {
            Monitor.Enter(_opQueue);
            try
            {
                if (_waitingThreads != 0)
                {
                    _waitingThreads = 0;
                    Monitor.PulseAll(_opQueue);
                }
            }
            finally
            {
                Monitor.Exit(_opQueue);
            }
        }
    }

    /// <summary>
    /// 核心工作循环：让真实线程死循环在队列中，如果没有任务就无损耗挂起
    /// </summary>
    public long Run()
    {
        long processCount = 0;
        while (_isRunning)
        {
            Monitor.Enter(_opQueue);
            try
            {
                if (_opQueue.Count != 0)
                {
                    var firstNode = _opQueue.First!;
                    _opQueue.RemoveFirst();
                    Monitor.Exit(_opQueue); // 极佳微操：提取完指针即立刻出锁，腾出通道让外界继续 Post

                    processCount++;
                    // 工业级安全调用，防止用户回调抛异常导致整个调度机崩溃
                    Functional.CatchInvoke(firstNode.value);
                }
                else if (_workCount != 0)
                {
                    // 货架空了，但由于外部保护计数不为0（证明后续还有硬件轮询或网络包要进场）
                    // 物理线程立刻交出 CPU 占有率，进入无损耗绝对沉睡状态
                    _waitingThreads++;
                    Monitor.Wait(_opQueue);
                }
                else
                {
                    // 货架已空，且保护计数归零，说明整个系统要停机，线程安全退出死循环
                    break;
                }
            }
            finally
            {
                // 确保在各种提前退出或异常情况下，锁都能被释放
                if (Monitor.IsEntered(_opQueue))
                {
                    Monitor.Exit(_opQueue);
                }
            }
        }
        return processCount;
    }

    public void Stop() => _isRunning = false;
    public void Reset() => _isRunning = true;
    public int Count => _opQueue.Count;

    public WorkGuard CreateWorkGuard() => new(this);
}
