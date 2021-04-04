using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib
{
    public class TracerTimer
    {
        private readonly ITracer _tracer;
        private readonly Stopwatch _watch = new Stopwatch();

        public TracerTimer(ITracer tracer)
        {
            _tracer = tracer;
            _watch.Start();
        }

        public void WriteTime(bool isVerbose, string text)
        {
            _tracer.WriteLine(isVerbose, $"{text} - {_watch.Elapsed}");
        }
    }
}