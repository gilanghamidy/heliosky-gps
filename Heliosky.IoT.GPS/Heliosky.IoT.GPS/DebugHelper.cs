using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Heliosky.IoT.GPS
{
    public static class DebugHelper
    {
        public static void PrintArray(byte[] arr)
        {
            StringBuilder bldr = new StringBuilder();
            bldr.Append("Array content:");
            for(int i = 0; i < arr.Length; i++)
            {
                if (i % 8 == 0)
                    bldr.AppendLine();

                bldr.Append(arr[i].ToString("X"));
                bldr.Append(" ");
            }

            Debug.WriteLine(bldr.ToString());
        }   
    }
}
