using Go.Runtime.Collections;
using System;
using System.Collections.Generic;
using System.Text;

namespace Go.Runtime.Tests.Collections;

public class MsgQueueTests
{
    [Fact]
    public void MsgQueueNode_Create_ShouldInitializeCorrectly()
    {
        int expectedValue = 42;

        var node = new MsgQueueNode<int>(expectedValue);

        Assert.Equal(expectedValue, node.Value);
        Assert.Null(node.next);
    }

    [Fact]
    public void MsgQueue_AddLast_And_RemoveFirst_ShouldMaintainStrictFIFO()
    {
        var queue = new MsgQueue<string>();
        var node1 = new MsgQueueNode<string>("Task_1");
        var node2 = new MsgQueueNode<string>("Task_2");

        queue.AddLast(node1);
        queue.AddLast(node2);

        Assert.Equal(2, queue.Count);
        Assert.Same(node1, queue.First);
        Assert.Same(node2, queue.Last);
        Assert.Same(node2, node1.next);

        queue.RemoveFirst();
        Assert.Equal(1, queue.Count);
        Assert.Same(node2, queue.First);

        queue.RemoveFirst();
        Assert.Equal(0, queue.Count);
        Assert.Null(queue.First);
        Assert.Null(queue.Last);
    }

    [Fact]
    public void MsgQueue_Clear_ShouldResetAllPointers()
    {
        var queue = new MsgQueue<int>();
        queue.AddLast(new MsgQueueNode<int>(1));
        queue.AddLast(new MsgQueueNode<int>(2));

        queue.Clear();

        Assert.Equal(0, queue.Count);
        Assert.Null(queue.First);
        Assert.Null(queue.Last);
    }
}
