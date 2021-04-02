namespace DeltaKustoLib
{
    public interface ITracer
    {
        void WriteLine(bool isVerbose, string text);

        void WriteErrorLine(string text);
    }
}