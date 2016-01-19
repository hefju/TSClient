using System;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Threading;



namespace TSClient
{
    class Program
    {
        //希望c++端都能做成dll给c#调用.
        [DllImport("Kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr OpenEvent(UInt32 dwDesiredAccess, Boolean bInheritHandle, String lpName);
        [DllImport("kernel32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SetEvent(IntPtr hEvent);
        static void Main(string[] args)
        {
            uint unEventPermissions = 2031619; // Same as EVENT_ALL_ACCESS value in the Win32 realm
            IntPtr hEvent = IntPtr.Zero;
            hEvent = OpenEvent(unEventPermissions, false, "ClientReadOver");
            if (IntPtr.Zero == hEvent)
            {
                Console.WriteLine("OpenEvent: ClientReadOver failed! \n(press any key to continue.)");
                Console.ReadKey();
                return; // Exit
            }

            using (var mmf = MemoryMappedFile.OpenExisting("ShareMemoryTest"))
            {
                long capacity = 1 << 10 << 10;
                MemoryMappedViewAccessor viewAccessor = mmf.CreateViewAccessor(0, capacity);

                while (true)
                {
                    SetEvent(hEvent);//设置事件,但是如果不成功会怎样? 
                    ProcessCommNoticeWarningInfo m_Info2;//结构体

                    int offset = 8; //memcpy时候把信息都写到pmemcomm里面了.
                    viewAccessor.Read(offset, out m_Info2);    // var type=viewAccessor.ReadInt32(offset); 逐个字段去读取
                    int strLength = m_Info2.msglen; //char[] message的长度
                    byte[] byteArray = new byte[strLength];//c++的char不能对应c#的char, 只能用byte来处理
                    viewAccessor.ReadArray<byte>(offset + 16, byteArray, 0, strLength);
                    Console.WriteLine("type:{0},module:{1},warning_num:{2},msglen:{3}", m_Info2.type, m_Info2.module, m_Info2.warning_num, m_Info2.msglen);
                    Console.WriteLine(System.Text.Encoding.Default.GetString(byteArray));
                    Thread.Sleep(2500);
                }
            }
        }//end of mian
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct ProcessCommNoticeWarningInfo
    {
        public Int32 type;//1提示信息2报警信息可以更大的值按调试等级进行显示
        public Int32 module;//0机械手1源板2目标板3多功能头4光学
        public Int32 warning_num;//报警编号
        public Int32 msglen;//消息长度
        //  public char[] message;//提示信息或报警说明  //c#错误: 指定的类型必须是不包含引用的结构。
    }

}
