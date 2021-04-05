using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib
{
    public class ConsoleTracer : ITracer
    {
        private readonly bool _doTraceVerbose;

        public ConsoleTracer(bool doTraceVerbose)
        {
            _doTraceVerbose = doTraceVerbose;
        }

        void ITracer.WriteLine(bool isVerbose, string text)
        {
            if (!isVerbose || _doTraceVerbose)
            {
                if(isVerbose)
                {
                    Console.Write($"[Verbose - {DateTime.Now.ToString("HH:mm:ss.f")}]  ");
                }
                Console.WriteLine(text);
            }
        }

        void ITracer.WriteErrorLine(string text)
        {
            Console.Error.WriteLine(text);
        }
    }
}