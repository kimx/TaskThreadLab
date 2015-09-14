using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TaskThreadLab
{
    /// <summary>
    /// reference:http://blog.darkthread.net/post-2012-07-20-net4-task.aspx
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            //test1();
            //test2();
            //test3();
            //test4();
            //test5();
            test6();
            Console.ReadLine();
        }

        /// <summary>
        /// 先從最簡單的開始。test1()用以另一條Thread執行Thread.Sleep()及Console.WriteLine()，效果與ThreadPool.QueueUserWorkItem()相當。
        /// </summary>
        static void test1()
        {
            //Task可以代替TheadPool.QueueUserWorkItem使用
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("test1:Done!");

            });
            Console.WriteLine("test1:Async Run...");
        }

        /// <summary>
        /// 同時啟動數個作業多工並行，但要等待各作業完成再繼續下一步是常見的應用情境，傳統上可透過WaitHandle、AutoResetEvent、ManualResetEvent等機制實現；
        /// Task的寫法相對簡單，建立多個Task物件，再當成Task.WaitAny()或Task.WaitAll()的參數就搞定囉!
        /// </summary>
        static void test2()
        {
            var task1 = Task.Factory.StartNew(() =>
              {
                  Thread.Sleep(3000);
                  Console.WriteLine("test2:Done! 3s");
              });

            var task2 = Task.Factory.StartNew(() =>
             {
                 Thread.Sleep(5000);
                 Console.WriteLine("test2:Done! 5s");
             });
            //等待任一作業完成後繼續
            Task.WaitAny(task1, task2);
            Console.WriteLine("test2:WaitAny Passed");
            //等待兩項作業都完成才會繼續執行
            Task.WaitAll(task1, task2);
            Console.WriteLine("test2:WaitAll Passed");
        }

        /// <summary>
        /// 如果要等待多工作業傳回結果，透過StartNew<T>()指定傳回型別建立作業，隨後以Task.Result取值，不用額外寫Code就能確保多工作業執行完成後才讀取結果繼續運算。
        /// </summary>
        static void test3()
        {
            var task = Task.Factory.StartNew<string>(() =>
            {
                Thread.Sleep(1000);
                return "Done!";
            });
            //使用馬錶計時
            Stopwatch sw = new Stopwatch();
            sw.Start();
            //讀task.Result時，會等到作業完畢傳回值後才繼續
            Console.WriteLine("test3:{0}", task.Result);
            sw.Stop();
            //要取得task.Result耗時約2秒
            Console.WriteLine("test3:Duration: {0:N0}ms", sw.ElapsedMilliseconds);
        }

        /// <summary>
        /// 如果要安排多工作業完成後接連執行另一段程式，可使用ContinueWith():
        /// </summary>
        static void test4()
        {
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("Done");
            }).ContinueWith(task =>
            {
                Console.WriteLine("In ContinueWith");
            });

            Console.WriteLine("Async Run...");
        }

        static void test5()
        {
            //ContinueWith()可以串接
            Task.Factory.StartNew(() =>
            {
                Thread.Sleep(2000);
                Console.WriteLine("{0:mm:ss}-Done", DateTime.Now);
            })
          .ContinueWith(task =>
          {
              Console.WriteLine("{0:mm:ss}-ContinueWith 1", DateTime.Now);
              Thread.Sleep(2000);
          })
          .ContinueWith(task =>
          {
              Console.WriteLine("{0:mm:ss}-ContinueWith 2", DateTime.Now);
          });
            Console.WriteLine("{0:mm:ss}-Async Run...", DateTime.Now);
        }

        static void test6()
        {
            CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken cancelToken = cts.Token;
            Console.Write("Test Option 1, 2 or 3 (1-Complete / 2-Cancel / 3-Fault) : ");
            var key = Console.ReadKey(); Console.WriteLine();
            Task.Factory.StartNew<string>(() =>
            {
                //保留5秒偵測是否要Cancel
                for (var i = 0; i < 5; i++)
                {
                    Thread.Sleep(1000);
                    //如cancelToken.IsCancellationRequested
                    //抛出OperationCanceledException
                    cancelToken.ThrowIfCancellationRequested();
                }
                switch (key.Key)
                {
                    case ConsoleKey.D1: //選1時
                        return "OK";
                    case ConsoleKey.D3: //選3時
                        throw new ApplicationException("MyException");
                }
                return "Unknown Input";
            }, cancelToken).ContinueWith(task =>
            {
                Console.WriteLine("IsCompleted: {0} IsCanceled: {1} IsFaulted: {2}",
                    task.IsCompleted, task.IsCanceled, task.IsFaulted);
                if (task.IsCanceled)
                {
                    Console.WriteLine("Canceled!");
                }
                else if (task.IsFaulted)
                {
                    Console.WriteLine("Faulted!");
                    foreach (Exception e in task.Exception.Flatten().InnerExceptions)
                    {
                        Console.WriteLine("Error: {0}", e.Message);
                    }
                }
                else if (task.IsCompleted)
                {
                    Console.WriteLine("Completed! Result={0}", task.Result);
                }
            });
            Console.WriteLine("Async Run...");
            //如果要測Cancel，2秒後觸發CancellationTokenSource.Cancel
            if (key.Key == ConsoleKey.D2)
            {
                Thread.Sleep(2000);
                cts.Cancel();
            }
        }
    }
}
