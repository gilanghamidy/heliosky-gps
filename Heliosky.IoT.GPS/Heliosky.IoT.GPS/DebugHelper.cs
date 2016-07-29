/*   DebugHelper.cs
 *   Copyright (C) 2016 Gilang M. Hamidy (gilang.hamidy@gmail.com)
 *   
 *   This file is part of Heliosky.IoT.GPS
 * 
 *   Heliosky.IoT.GPS is free software: you can redistribute it and/or modify
 *   it under the terms of the GNU Lesser General Public License as published by
 *   the Free Software Foundation, either version 3 of the License, or
 *   (at your option) any later version.
 *
 *   Heliosky.IoT.GPS is distributed in the hope that it will be useful,
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *   GNU Lesser General Public License for more details.
 *
 *   You should have received a copy of the GNU Lesser General Public License
 *   along with Heliosky.IoT.GPS.  If not, see <http://www.gnu.org/licenses/>.
 */


using System.Diagnostics;
using System.Text;

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
