using System;
using System.Diagnostics;

namespace Go.Runtime.Common;

public class Functional
{
    public static void CatchInvoke(Action handler)
    {
		try
		{
			handler?.Invoke();
		}
		catch (Exception ex)
		{
			Trace.Fail(ex.Message, ex.StackTrace);
		}
    }
}
