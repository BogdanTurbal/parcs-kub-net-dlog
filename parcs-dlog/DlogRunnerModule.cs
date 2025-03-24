using Parcs.Net;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace parcs.Dlog
{
    // Test case model using long parameters.
    public class TestCase
    {
        public string Name { get; }
        public long P { get; }
        public long G { get; }
        public long H { get; }
        public long Secret { get; }

        public TestCase(string name, long p, long g, long h, long secret)
        {
            Name = name;
            P = p;
            G = g;
            H = h;
            Secret = secret;
        }
    }

    public class DlogRunnerModule : IModule
    {
        public async Task RunAsync(IModuleInfo moduleInfo, CancellationToken cancellationToken = default)
        {
            var testCases = new List<TestCase>
            {
                new TestCase("Test 1: 28-bit", 155641523, 2, 50221314, 18492822),
                new TestCase("Test 2: 28-bit", 209433947, 2, 164525372, 16743737),
                new TestCase("Test 3: 28-bit", 242259383, 5, 19985811, 51354402),
                new TestCase("Test 4: 30-bit", 414414179, 2, 393725970, 122935119),
                new TestCase("Test 5: 30-bit", 768948143, 5, 552396870, 150337452),
                new TestCase("Test 6: 30-bit", 236633399, 7, 167645516, 33810309),
                new TestCase("Test 7: 32-bit", 259740347, 2, 177317688, 20581127),
                new TestCase("Test 8: 32-bit", 1458997979, 2, 261414634, 416521929),
                new TestCase("Test 9: 32-bit", 3484181747, 2, 3133887145, 1023830831),
                new TestCase("Test 10: 34-bit", 7937997059, 2, 5515183604, 1185754458),
                new TestCase("Test 11: 34-bit", 10923184463, 5, 1083890450, 3940883458),
                new TestCase("Test 12: 34-bit", 10542839327, 5, 8689873404, 4723704221),
            };
            int[] workerCounts = new int[] { 1, 2, 4 };

            StringBuilder output = new StringBuilder();
            output.AppendLine("Test,Prime,G,Secret,H,Workers,TimeSeconds,FoundSolution");

            Dictionary<int, List<double>> batchTimes = workerCounts.ToDictionary(w => w, w => new List<double>());
            int testCounter = 0;

            foreach (var test in testCases)
            {
                testCounter++;
                output.AppendLine(new string('-', 100));
                output.AppendLine($"Starting {test.Name}: p={test.P}, g={test.G}, h={test.H}, secret={test.Secret}");

                foreach (int workers in workerCounts)
                {
                    output.AppendLine($"{test.Name} with {workers} worker(s)");

                    IPoint[] points = new IPoint[workers];
                    IChannel[] channels = new IChannel[workers];
                    for (int i = 0; i < workers; i++)
                    {
                        points[i] = await moduleInfo.CreatePointAsync();
                        channels[i] = await points[i].CreateChannelAsync();
                    }

                    for (int workerIndex = 0; workerIndex < workers; workerIndex++)
                    {
                        await points[workerIndex].ExecuteClassAsync<DlogWorkerModule>();
                        await channels[workerIndex].WriteDataAsync(test.P);
                        await channels[workerIndex].WriteDataAsync(test.G);
                        await channels[workerIndex].WriteDataAsync(test.H);
                        await channels[workerIndex].WriteDataAsync((long)workerIndex); 
                        await channels[workerIndex].WriteDataAsync(test.P);          
                        await channels[workerIndex].WriteDataAsync((long)workers);   
                    }

                    var sw = Stopwatch.StartNew();

                    var tasks = new List<Task<(bool Found, long? Solution, int WorkerIndex)>>();
                    for (int i = 0; i < workers; i++)
                    {
                        int currentWorker = i; 
                        tasks.Add(Task.Run(async () =>
                        {
                            bool found = await channels[currentWorker].ReadBooleanAsync();
                            long? solution = null;
                            if (found)
                            {
                                solution = await channels[currentWorker].ReadLongAsync();
                            }
                            return (Found: found, Solution: solution, WorkerIndex: currentWorker);
                        }));
                    }

                    double elapsed = 0;
                    (bool Found, long? Solution, int WorkerIndex) result = (false, null, -1);
                    while (tasks.Any())
                    {
                        var completedTask = await Task.WhenAny(tasks);
                        sw.Stop();
                        elapsed = sw.Elapsed.TotalSeconds;

                        tasks.Remove(completedTask);
                        var res = await completedTask;
                        if (res.Found)
                        {
                            result = res;

                            break;
                        }
                    }

                    if (!result.Found)
                    {
                        output.AppendLine($"{test.Name} with {workers} worker(s) - No solution found in {elapsed:F2} seconds");
                    }
                    else
                    {
                        output.AppendLine($"{test.Name} with {workers} worker(s) completed in {elapsed:F2} seconds");
                        if (result.Solution.HasValue && result.Solution.Value == test.Secret)
                        {
                            output.AppendLine("Verification successful: found solution matches expected secret.");
                        }
                        else
                        {
                            output.AppendLine($"Verification FAILED: expected {test.Secret}, got {result.Solution}");
                        }
                    }

                    output.AppendLine($"{test.Name},{test.P},{test.G},{test.Secret},{test.H},{workers},{elapsed:F2},{result.Solution}");
                    batchTimes[workers].Add(elapsed);

                    await Task.Delay(5000);
                    output.AppendLine();
                }

                if (testCounter % 3 == 0)
                {
                    foreach (int workers in workerCounts)
                    {
                        double avg = batchTimes[workers].Average();
                        output.AppendLine($"Mean execution time for {workers} worker(s) over tests {testCounter - 2} to {testCounter}: {avg:F2} seconds");
                        batchTimes[workers].Clear();
                    }
                }
            }

            output.AppendLine("All experiments completed.");
            await moduleInfo.OutputWriter.WriteToFileAsync(Encoding.UTF8.GetBytes(output.ToString()), "DlogResults.txt");
        }
    }
}
