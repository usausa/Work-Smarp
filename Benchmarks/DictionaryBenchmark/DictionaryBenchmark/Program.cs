﻿using System;
using System.Diagnostics;
using System.Reflection;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Smart.Collections.Concurrent;

namespace DictionaryBenchmark
{
    public class Program
    {
        private static void Test()
        {
            var hashArrayMap = new ThreadsafeIntHashArrayMap<object>(16, 1);

            for (var i = 0; i < 1024; i++)
            {
                Debug.Assert(hashArrayMap.TryGetValue(i, out _) == false);
                hashArrayMap.AddIfNotExist(i, new object());

                for (var j = 0; j < 1024; j++)
                {
                    if (!hashArrayMap.TryGetValue(j, out _))
                    {
                        Debug.WriteLine("--");
                        hashArrayMap.Dump();
                    }

                    if (i == j)
                    {
                        break;
                    }
                }

                Debug.Assert(hashArrayMap.Count == i + 1);
                Debug.Assert(hashArrayMap.Depth == 1);
            }
        }


        public static void Main(string[] args)
        {
            Test();

            // TODO
            var hashArrayMap = new ThreadsafeTypeHashArrayMap<object>();
            foreach (var type in Classes.Types)
            {
                if (type == typeof(Class12) || type == typeof(Class13))
                {
                    Debug.WriteLine("--");
                }

                Debug.Assert(hashArrayMap.TryGetValue(type, out _) == false);
                hashArrayMap.AddIfNotExist(type, new object());

                //Debug.WriteLine("--");
                //hashArrayMap.Dump();

                foreach (var type2 in Classes.Types)
                {
                    if (!hashArrayMap.TryGetValue(type2, out _))
                    {
                        Debug.WriteLine("--");
                        hashArrayMap.Dump();
                        Debug.WriteLine(type2.Name);
                    }

                    if (type == type2)
                    {
                        break;
                    }
                }

                //Debug.WriteLine("--");
                //hashArrayMap.Dump();
                Debug.WriteLine(hashArrayMap.Count + " " + hashArrayMap.Depth);
            }

            hashArrayMap.Dump();

            foreach (var type in Classes.Types)
            {
                if (!hashArrayMap.TryGetValue(type, out _))
                {
                    Debug.Assert(hashArrayMap.TryGetValue(type, out _));
                }
            }

            BenchmarkSwitcher.FromAssembly(typeof(Program).GetTypeInfo().Assembly).Run(args);
        }
    }

    public class BenchmarkConfig : ManualConfig
    {
        public BenchmarkConfig()
        {
            Add(MarkdownExporter.Default, MarkdownExporter.GitHub);
            Add(MemoryDiagnoser.Default);
            Add(Job.MediumRun);
        }
    }

    [Config(typeof(BenchmarkConfig))]
    public class Benchmark
    {
        private readonly ThreadsafeTypeHashArrayMap<object> hashArrayMap = new ThreadsafeTypeHashArrayMap<object>();

        private readonly ThreadsafeTypeHashArrayMapOld<object> hashArrayMapOld = new ThreadsafeTypeHashArrayMapOld<object>();

        private readonly Type key = typeof(Class0);

        [GlobalSetup]
        public void Setup()
        {
            foreach (var type in Classes.Types)
            {
                hashArrayMap.AddIfNotExist(type, type);
                hashArrayMapOld.AddIfNotExist(type, type);
            }
        }

        [Benchmark]
        public object ThreadsafeTypeHashArrayMap()
        {
            return hashArrayMap.TryGetValue(key, out var obj) ? obj : null;
        }

        [Benchmark]
        public object ThreadsafeTypeHashArrayMapOld()
        {
            return hashArrayMapOld.TryGetValue(key, out var obj) ? obj : null;
        }
    }
}
