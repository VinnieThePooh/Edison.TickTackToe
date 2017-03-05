using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KoloroturConsoleTime
{
    class Program
    {
        // навеяно
        //http://www.cyberforum.ru/csharp-beginners/thread1932206.html
        //Задача состоит в том, чтобы выводить время на консоль в правом верхнем углу, 
        //при этом программа не должна увеличивать количество потребляемой оперативной памяти, а также нагружать процессор более, чем на 0.5%
        static void Main(string[] args)
        {
            var buffer = new char[79];
            buffer[0] = '\r';

            while (true)
            {
                var now = DateTime.Now;

                buffer[71] = (char)('0' + (now.Hour / 10));
                buffer[72] = (char)('0' + (now.Hour % 10));
                buffer[73] = ':';
                buffer[74] = (char)('0' + (now.Minute / 10));
                buffer[75] = (char)('0' + (now.Minute % 10));
                buffer[76] = ':';
                buffer[77] = (char)('0' + (now.Second / 10));
                buffer[78] = (char)('0' + (now.Second % 10));

                Console.Write(buffer);
                Thread.Sleep(100);
            }
        }
    }
}
