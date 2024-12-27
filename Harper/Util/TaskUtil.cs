using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Harper.Util;

public static class TaskUtil
{
    public static IEnumerable<Task> ForEachTask<T>(Func<T, Task> exec, IEnumerable<T> objs)
    {
        foreach (T obj in objs)
            yield return exec.Invoke(obj);
    }
}
