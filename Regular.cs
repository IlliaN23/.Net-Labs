using System;
using System.Diagnostics;
using System.Threading;

namespace VectorAddition.MultiThread
{
    /// <summary>
    /// Клас для представлення вектора
    /// </summary>
    public class Vector
    {
        private double[] elements;

        public int Size => elements.Length;

        public Vector(int size)
        {
            if (size <= 0)
                throw new ArgumentException("Розмір вектора має бути більше 0");
            elements = new double[size];
        }

        public double this[int index]
        {
            get => elements[index];
            set => elements[index] = value;
        }

        public void FillRandom(Random random, double min = -100, double max = 100)
        {
            for (int i = 0; i < elements.Length; i++)
            {
                elements[i] = random.NextDouble() * (max - min) + min;
            }
        }

        public bool Equals(Vector? other)
        {
            if (other == null || Size != other.Size)
                return false;

            for (int i = 0; i < Size; i++)
            {
                if (Math.Abs(elements[i] - other[i]) > 1e-9)
                    return false;
            }
            return true;
        }
    }

    /// <summary>
    /// Клас для однопотокового додавання векторів
    /// </summary>
    public class SingleThreadVectorAdder
    {
        public Vector Add(Vector a, Vector b)
        {
            if (a == null || b == null)
                throw new ArgumentNullException("Вектори не можуть бути null");

            if (a.Size != b.Size)
                throw new ArgumentException("Розміри векторів мають співпадати");

            Vector result = new Vector(a.Size);

            for (int i = 0; i < a.Size; i++)
            {
                result[i] = a[i] + b[i];
            }

            return result;
        }

        public long AddWithTiming(Vector a, Vector b, out Vector result)
        {
            Stopwatch sw = Stopwatch.StartNew();
            result = Add(a, b);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    /// <summary>
    /// Клас для багатопотокового додавання векторів
    /// </summary>
    public class MultiThreadVectorAdder
    {
        private int threadCount;

        public int ThreadCount
        {
            get => threadCount;
            set
            {
                if (value <= 0)
                    throw new ArgumentException("Кількість потоків має бути більше 0");
                threadCount = value;
            }
        }

        public MultiThreadVectorAdder(int threadCount)
        {
            ThreadCount = threadCount;
        }

        /// <summary>
        /// Структура для передачі параметрів у потік
        /// </summary>
        private class ThreadData
        {
            public Vector? VectorA { get; set; }
            public Vector? VectorB { get; set; }
            public Vector? Result { get; set; }
            public int StartIndex { get; set; }
            public int EndIndex { get; set; }
            public Exception? Error { get; set; }
        }

        /// <summary>
        /// Метод, що виконується у окремому потоці
        /// </summary>
        private void AddVectorSegment(object? obj)
        {
            if (obj == null) return;

            ThreadData data = (ThreadData)obj;

            try
            {
                if (data.VectorA != null && data.VectorB != null && data.Result != null)
                {
                    for (int i = data.StartIndex; i < data.EndIndex; i++)
                    {
                        data.Result[i] = data.VectorA[i] + data.VectorB[i];
                    }
                }
            }
            catch (Exception ex)
            {
                data.Error = ex;
            }
        }

        public Vector Add(Vector a, Vector b)
        {
            if (a == null || b == null)
                throw new ArgumentNullException("Вектори не можуть бути null");

            if (a.Size != b.Size)
                throw new ArgumentException("Розміри векторів мають співпадати");

            Vector result = new Vector(a.Size);
            int size = a.Size;

            // Визначаємо розмір сегмента для кожного потоку
            int segmentSize = (int)Math.Ceiling(size / (double)threadCount);

            Thread[] threads = new Thread[threadCount];
            ThreadData[] threadDataArray = new ThreadData[threadCount];

            // Створюємо та запускаємо потоки
            for (int i = 0; i < threadCount; i++)
            {
                int startIndex = i * segmentSize;
                int endIndex = Math.Min(startIndex + segmentSize, size);

                if (startIndex >= size)
                    break;

                ThreadData data = new ThreadData
                {
                    VectorA = a,
                    VectorB = b,
                    Result = result,
                    StartIndex = startIndex,
                    EndIndex = endIndex
                };

                threadDataArray[i] = data;
                threads[i] = new Thread(AddVectorSegment);
                threads[i].Start(data);
            }

            // Чекаємо завершення всіх потоків
            for (int i = 0; i < threadCount; i++)
            {
                if (threads[i] != null)
                {
                    threads[i].Join();

                    if (threadDataArray[i]?.Error != null)
                        throw threadDataArray[i].Error;
                }
            }

            return result;
        }

        public long AddWithTiming(Vector a, Vector b, out Vector result)
        {
            Stopwatch sw = Stopwatch.StartNew();
            result = Add(a, b);
            sw.Stop();
            return sw.ElapsedMilliseconds;
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.WriteLine("Environment.ProcessorCount = " + Environment.ProcessorCount);
            Console.WriteLine();

            // Заголовок таблиці
            Console.WriteLine($"{"Size",-12} {"Threads",-8} {"Single(s)",-14} {"Multi(s)",-12} {"Speedup",-10} {"Correct"}");

            int[] sizes = { 100000, 500000, 1000000, 5000000, 10000000, 50000000 };
            int processorCount = Environment.ProcessorCount;

            foreach (int size in sizes)
            {
                RunTests(size, processorCount);
            }

            Console.WriteLine();
            Console.WriteLine("Натисніть будь-яку клавішу для завершення...");
            Console.ReadKey();
        }

        static void RunTests(int size, int maxThreads)
        {
            Random random = new Random(42);

            Vector v1 = new Vector(size);
            Vector v2 = new Vector(size);

            v1.FillRandom(random);
            v2.FillRandom(random);

            // Однопотокова версія (для еталону)
            SingleThreadVectorAdder singleAdder = new SingleThreadVectorAdder();

            // Прогрів
            Vector temp;
            singleAdder.AddWithTiming(v1, v2, out temp);

            // Вимірювання однопотокової версії
            const int iterations = 5;
            long singleTotalTime = 0;
            Vector? singleResult = null;

            for (int i = 0; i < iterations; i++)
            {
                Vector result;
                long time = singleAdder.AddWithTiming(v1, v2, out result);
                singleTotalTime += time;
                if (singleResult == null)
                    singleResult = result;
            }

            double avgSingleTime = singleTotalTime / (double)iterations;
            double avgSingleTimeSeconds = avgSingleTime / 1000.0;

            // Тестування багатопотокової версії з різною кількістю потоків
            int[] threadCounts = { 1, 2, 4, 8, 16, maxThreads };

            foreach (int tc in threadCounts)
            {
                if (tc > maxThreads && tc != 16)
                    continue;

                MultiThreadVectorAdder multiAdder = new MultiThreadVectorAdder(tc);

                // Прогрів
                Vector warmup;
                multiAdder.AddWithTiming(v1, v2, out warmup);

                // Вимірювання
                long multiTotalTime = 0;
                Vector? multiResult = null;

                for (int i = 0; i < iterations; i++)
                {
                    Vector result;
                    long time = multiAdder.AddWithTiming(v1, v2, out result);
                    multiTotalTime += time;
                    if (multiResult == null)
                        multiResult = result;
                }

                double avgMultiTime = multiTotalTime / (double)iterations;
                double avgMultiTimeSeconds = avgMultiTime / 1000.0;
                double speedup = avgSingleTime / avgMultiTime;
                bool correct = singleResult != null && multiResult != null && singleResult.Equals(multiResult);

                // Форматований вивід
                Console.WriteLine($"{size,-12} {tc,-8} {avgSingleTimeSeconds,-14:F1} {avgMultiTimeSeconds,-12:F1} {speedup,-10:F3} {correct}");
            }
        }
    }
}