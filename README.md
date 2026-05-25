## 🔗 致谢与源码来源 (Acknowledge & Credits)

本项目的并发内核、时序调度算法及零分配数据结构的底层逻辑，深度参考/致谢自开源项目：
* **原始项目 (Original Repository)**: [CsGo (C# Concurrency Runtime like Go)](https://github.com/HAM-2015/CsGo)
* **原作者 (Original Author)**: [HAM-2015](https://github.com/HAM-2015)

> **版权与免责声明**：本项目非原仓库的简单 Fork。本项目纯粹用于个人技术的纵向探索与重构练习。所有重写代码均在遵循开源规范的前提下，加入现代 `.NET` 特性重构并进行独立演进。

---

## 🎯 建立本仓库的学习意图 (Learning Objectives)

原项目 `CsGo` 是一套非常硬核的、在 `.NET` 平台上用 C# 指针和内核对象硬揉出来的**高性能并发运行时**，它完美地在底层复刻了 Go 语言的 CSP 并发模型。

为了摆脱“写玩具”的传统业务层思维，向工业级基础设施研发靠拢，本仓库建立的初衷在于通过**“由内向外的剥洋葱式重构”**，彻底攻克以下三大技术壁垒：

### 1. 空间域：零分配（Zero-Allocation）数据结构
* **探索本质**：研究原作者自研的 `MsgQueue`（单向高速链表）与带有 `ReNewNode` 原地改写机制的红黑树（`Map`）。
* **重构目标**：掌握在 C# 里进行指针级、无垃圾引入的引用交换，从底层杜绝工业运控系统中最忌讳的 .NET GC（垃圾回收）卡顿。

### 2. 时间域：高精度内核调度引擎
* **探索本质**：攻克 Windows 时钟盲区，深入研究 `ntdll.dll` 未公开函数（`NtSetTimerResolution`）对系统时钟分辨率的压榨，以及基于硬件芯片晶振脉冲计数（QPC）的微秒级计时。
* **重构目标**：掌握 Windows 内核级可等待定时器对象（`WaitableTimer`）的线程挂起与刺醒机制。

### 3. 线程域：无锁化串行线索（Strand 模式）
* **探索本质**：彻底搞懂类似 Boost.Asio 核心的 `shared_strand` 调度逻辑。
* **重构目标**：利用双队列（Ready/Wait Queue）地址闪电互换，实现“多线程高频投递，单线索安全执行”，在零加锁的前提下彻底解决工控通信（如 PLC 读写）中的时序错乱与粘包问题。

---

## 🛡️ 本仓库的严谨工程化标准 (Engineering Standards)

为了将本次挑战提升至工业级基础设施交付的标准，本仓库推翻了原项目类似 C++ 的命名风格与旧版 `.NET` 的历史包袱，强制引入以下现代工程化武器：

* **测试驱动开发 (TDD)**：引入 `xUnit` 自动化测试。底层任何指针移动、链表重组，必须首先通过严格的单元测试断言，彻底消除任何确定性盲区。
* **基准性能测试 (Benchmark)**：引入官方 `BenchmarkDotNet`。对重构后的数据结构进行吞吐量和内存分配（Allocated Memory）的微秒级定量分析。
* **现代 .NET 8/9 AOT 严苛编译**：
  * 全面开启 `<Nullable>enable</Nullable>`（从编译期消灭空指针异常）。
  * 全面开启 `<IsAotCompatible>true</IsAotCompatible>`（强行约束代码适配 NativeAOT 原生 Ahead-of-Time 极速编译，为自研零 GC 通信协议库奠定地基）。
