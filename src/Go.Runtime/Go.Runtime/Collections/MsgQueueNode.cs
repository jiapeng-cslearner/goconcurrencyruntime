using System;
using System.Collections.Generic;
using System.Text;

namespace Go.Runtime.Collections;

/// <summary>
/// 高速单向链表队列节点
/// </summary>
public class MsgQueueNode<T>
{
    internal T value;
    internal MsgQueueNode<T>? next;

    public MsgQueueNode(T value)
    {
        this.value = value;
        this.next = null;
    }

    public T Value
    {
        get => value;
        set => this.value = value;
    }

    public MsgQueueNode<T>? Next
    {
        get => next;
    }
}
