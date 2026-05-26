using System;
using System.Collections.Generic;
using System.Text;

namespace Go.Runtime.Collections;

/// <summary>
/// 串行事件/指令队列 (彻底剥离官方 Queue 的数组扩容复制损耗)
/// </summary>
public class MsgQueue<T>
{
    private int _count;
    private MsgQueueNode<T>? _head;
    private MsgQueueNode<T>? _tail;

    public MsgQueue()
    {
        _count = 0;
        _head = null;
        _tail = null;
    }

    public void AddLast(MsgQueueNode<T> node)
    {
        if (_tail == null)
        {
            _head = node;
        }
        else
        {
            _tail.next = node;
        }
        node.next = null;
        _tail = node;
        _count++;
    }

    public void RemoveFirst()
    {
        if (_head == null)
            return;
        _head = _head.next;
        if (--_count == 0)
        {
            _tail = null;
        }
    }

    public void Clear()
    {
        _count = 0;
        _head = null;
        _tail = null;
    }

    public MsgQueueNode<T>? First => _head;
    public MsgQueueNode<T>? Last => _tail;
    public int Count => _count;
}
