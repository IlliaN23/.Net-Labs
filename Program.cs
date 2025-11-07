using System;
using System.Diagnostics;

namespace VectorAddition.SingleThread
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

            // Однопотокова версія
            SingleThreadVectorAdder adder = new SingleThreadVectorAdder();

            // Прогрів
            Vector temp;
            adder.AddWithTiming(v1, v2, out temp);

            // Вимірювання
            const int iterations = 5;
            long totalTime = 0;

            for (int i = 0; i < iterations; i++)
            {
                Vector result;
                long time = adder.AddWithTiming(v1, v2, out result);
                totalTime += time;
            }

            double avgTime = totalTime / (double)iterations;
            double avgTimeSeconds = avgTime / 1000.0;

            // Виводимо результат для 1 потоку
            Console.WriteLine($"{size,-12} {1,-8} {avgTimeSeconds,-14:F1} {"-",-12} {"-",-10} {"True"}");
        }
    }
}﻿
