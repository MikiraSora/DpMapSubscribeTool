using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Persistences
{
    public interface IPersistence
    {
        ValueTask Save<T>(T obj);
        ValueTask<T> Load<T>() where T : new();
    }
}
