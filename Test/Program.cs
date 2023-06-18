using System.Collections.Concurrent;
using System.Diagnostics.Metrics;
using System.Globalization;
using System.Threading;

public class Program
{
    #region Initialization
    public static void CreateTxtFile(string filePath, string fileName)
    {
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
        }
        Console.WriteLine($"{fileName}.txt created successfully.");
    }

    public static string GetDataTxtFilePath()
    {
        var fileName = "data";
        var baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
        var filePath = Path.Combine(baseDirectory, $"{fileName}.txt");
        return filePath;
    }

    public static string GenerateRandomData()
    {
        var random = new Random();
        var letters = "abcdefghijklmnopqrstuvwxyz";
        var wordCount = 1000;
        var minWordLength = 3;
        var maxWordLength = 15;

        var randomData = string.Join(" ", Enumerable.Range(0, wordCount)
            .Select(_ =>
            {
                int wordLength = random.Next(minWordLength, maxWordLength + 1);
                return new string(Enumerable.Range(0, wordLength)
                    .Select(__ => letters[random.Next(letters.Length)])
                    .ToArray());
            }));

        return randomData;
    }

    public static void InitializeData(string filePath, string fileName)
    {
        CreateTxtFile(filePath, fileName);
        var randomData = GenerateRandomData();
        File.WriteAllText(filePath, randomData);
    }

    public static string ReadFile(string filePath, string fileName)
    {
        return File.ReadAllText(filePath);
    }

    public static async Task Initialize()
    {
        await WaitToEnterCriticalSection(1);

        Console.WriteLine("Waiting P1");
        var filePath = GetDataTxtFilePath();
        InitializeData(filePath, "data");
        Console.WriteLine("Finished P1");

        SignalNextProcess(2);
    }

    #endregion

    public static SemaphoreSlim Semaphore { get; set; } = new(1);
    public static int NextProcess { get; set; } = 1;
    public static ConcurrentDictionary<int, int> Counts { get; set; } = new();

    public static async Task WaitToEnterCriticalSection(int processNumber)
    {
        while (true)
        {
            await Semaphore.WaitAsync();
            if (NextProcess == processNumber)
            {
                return;
            }
            Semaphore.Release();
            await Task.Delay(10);
        }
    }

    public static void SignalNextProcess(int processNumber)
    {
        NextProcess = processNumber;
        Semaphore.Release();
    }

    public static void CountWords(int length, List<string> words)
    {
        int count = words.Count(word => word.Length == length);
        Counts[length] = count;
    }

    public static async Task ReadFIFOChannel()
    {
        Console.WriteLine("Waiting P2");
        await WaitToEnterCriticalSection(2);

        var filePath = GetDataTxtFilePath();
        var words = File.ReadAllText(filePath);

        // I used Tasks instead of Threads, it has more features than threads hope you don't mind
        var list = words.Split(" ").ToList();
        var maxTasks = 13;
        var tasks = new Task[maxTasks];
        var taskIndex = 0;
        var wordsLengthToCount = 2;
        while (taskIndex < maxTasks)
        {
            await Task.Delay(1);
            var task = Task.Run(() => CountWords(wordsLengthToCount, list));
            tasks[taskIndex++] = task;
            wordsLengthToCount++;
        }

        await Task.WhenAll(tasks);
        Console.WriteLine("Finished P2");
        SignalNextProcess(3);
    }

    public static async Task FindMax()
    {
        Console.WriteLine("Waiting P3");

        await WaitToEnterCriticalSection(3);
        int maxCount = Counts.Values.Max();

        var maxLengths = Counts.Where(pair => pair.Value == maxCount)
                               .Select(pair => pair.Key);

        foreach (int length in maxLengths)
        {
            int count = Counts[length];
            Console.WriteLine($"Words of length {length} were repeated {count} times.");
        }
        Console.WriteLine("Finished P3");

        SignalNextProcess(1);

    }

    public static async Task Main(string[] args)
    {
        var p1 = Task.Run(() => Initialize());
        var p2 = Task.Run(() => ReadFIFOChannel());
        var p3 = Task.Run(() => FindMax());

        await Task.WhenAll(p1, p2, p3);

    }

    public static void DisplayDictionary(ConcurrentDictionary<int, int> dict)
    {
        foreach (KeyValuePair<int, int> pair in dict)
        {
            Console.WriteLine($"Key: {pair.Key}, Value: {pair.Value}");
        }
    }

}
