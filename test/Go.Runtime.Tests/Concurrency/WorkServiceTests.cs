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
}
