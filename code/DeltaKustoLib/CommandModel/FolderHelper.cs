using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DeltaKustoLib.CommandModel
{
    internal static class FolderHelper
    {
        public static string Escape(string original)
        {   //  See https://stackoverflow.com/questions/1976007/what-characters-are-forbidden-in-windows-and-linux-directory-names
            var builder = new StringBuilder(original);

            builder.Replace('<', '_');
            builder.Replace('>', '_');
            builder.Replace(':', '_');
            builder.Replace('\"', '_');
            builder.Replace('\\', '_');
            builder.Replace('|', '_');
            builder.Replace('?', '_');
            builder.Replace('*', '_');

            return builder.ToString();
        }

        public static QuotedText Escape(QuotedText original)
        {
            return new QuotedText(Escape(original.Text));
        }
    }
}