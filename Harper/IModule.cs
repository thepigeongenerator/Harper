using System;
using System.Threading.Tasks;

namespace Harper;

public interface IModule : IDisposable
{
    public Task Start();
    public Task Stop();
}
