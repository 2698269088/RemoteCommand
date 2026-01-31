using System;

namespace Remote_Command
{
    /// <summary>
    /// 测试获取带路径的应用进程列表功能
    /// </summary>
    public class TestProcessWithPath
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Testing GetAppProcessListWithPaths...");
            string result = ProcessListProvider.GetAppProcessListWithPaths();
            Console.WriteLine(result);
            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();
        }
    }
}