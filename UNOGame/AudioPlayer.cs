using System;
using System.IO;
using System.Runtime.InteropServices;

namespace UNOGame
{
    internal static class AudioPlayer
    {
        [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
        private static extern int mciSendString(string command, string returnValue, int returnLength, IntPtr callback);

        public static void Play(string fileName)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "Audio", fileName);
            if (!File.Exists(path))
            {
                return;
            }

            string alias = "uno_" + Math.Abs(fileName.GetHashCode()).ToString();
            mciSendString("close " + alias, null, 0, IntPtr.Zero);
            mciSendString("open \"" + path + "\" type mpegvideo alias " + alias, null, 0, IntPtr.Zero);
            mciSendString("play " + alias + " from 0", null, 0, IntPtr.Zero);
        }
    }
}
