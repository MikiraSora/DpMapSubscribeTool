using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DpMapSubscribeTool.Services.Persistences
{
    public interface IPersistence
    {
        Task Save<T>(T obj);
        Task<T> Load<T>();
    }
}
