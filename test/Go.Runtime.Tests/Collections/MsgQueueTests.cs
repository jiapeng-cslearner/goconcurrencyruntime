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
        Assert.Null(node.Next);
    }
}
