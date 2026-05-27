using Go.Runtime.Concurrency;

namespace Go.Runtime.Tests.Concurrency;

public class WorkServiceTests
{
    [Fact]
    public void Post_ShouldSignalAndWakeUpWaitingThread()
    {
        var service = new WorkService();
        bool isTaskExecuted = false;
        var countdownEvent = new CountdownEvent(1); // 用于跨线程同步等待验证？

        // 让服务默认持有一个活跃工作计数，防止没有任务时线程直接死掉退出
        service.HoldWork();

        Thread backgroundService = new (() =>
        {
            service.Run();
        })
        {
            IsBackground = true,
        };
        //backgroundService.TrySetApartmentState(ApartmentState.STA);
        backgroundService.Start();

        // 故意让主线程沉睡，确保 backgroundService 线程此时因为“无物料”而进入沉睡
        Thread.Sleep(50);

        service.Post(() =>
        {
            isTaskExecuted = true;
            countdownEvent.Signal();    // 发出成功执行信号
        });

        bool isSignaled = countdownEvent.Wait(500);

        service.ReleaseWork();
        backgroundService.Join(500);

        Assert.True(isSignaled, "测试超时：Post 动作未能成功刺醒正在沉睡挂起的物理工作线程！");
        Assert.True(isTaskExecuted, "任务未能成功执行！");
    }

    [Fact]
    public void WorkGuard_ShouldAutomaticallyReleaseWorkAndExitRun_WhenDisposed()
    {
        var service = new WorkService();
        var threadStartedEvent = new ManualResetEventSlim(false);
        var threadExitedEvent = new CountdownEvent(1);
        
        // 【核心修正 1】：在物理线程诞生前，主线程抢先 HoldWork()
        // 确保物理线程只要敢进 Run()，就绝对无法直接 break 退出，必须在 Monitor.Wait 里乖乖沉睡
        service.HoldWork();

        Thread backgroundWorker = new (() =>
        {
            service.Post(() => { threadStartedEvent.Set(); });
            service.Run();
            threadExitedEvent.Signal();
        })
        {
            IsBackground = true
        };
        backgroundWorker.Start();

        // 2. 绝对安全地等待后台线程至少启动并进入工作循环
        bool startedOk = threadStartedEvent.Wait(500);
        Assert.True(startedOk, "测试失败：后台线程未能及时启动就位！");

        // 【终极修正 2】：无缝交接计数
        // 先用 guard 抢先 HoldWork()（此时计数变为 2）
        using (WorkService.WorkGuard guard = service.CreateWorkGuard())
        {
            // 随后，把我们在测试最开始手动加的那一次基础占坑释放掉（计数回归 1）
            // 这样，在 using 块内部，完全由 guard 一个人在独自守护计数！
            service.ReleaseWork();

            // 验证：此时在 using 内部，由于 guard 的存在，物理线程绝对不能退出
            // 如果 threadExitedEvent 的 CurrentCount 变成了 0，说明线程死了，断言就会失败
            Assert.Equal(1, threadExitedEvent.CurrentCount);
        }

        // Assert (断言验证自动释放与唤醒退出)
        // 此时计数彻底归零，Monitor.PulseAll 必须刺醒物理线程让其跳出 Run()
        // 规定时间内（500 ms）等待，否则返回 false（失败）
        bool isThreadExited = threadExitedEvent.Wait(500);

        backgroundWorker.Join(500);

        // 断言：物理线程必须是因为 using 的自动释放而被成功唤醒并安全退出的
        Assert.True(isThreadExited, "测试失败，WorkGuard 离开 Using 作用域后，未能自动释放保护计数并刺醒退出物理线程！");
    }
}
