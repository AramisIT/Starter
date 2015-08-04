using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AramisIDE.SolutionUpdating;
using WebSolutionUpdater.Helpers;

namespace WebSolutionUpdater
    {
    public class DetailedUpdateTask
        {
        public HashSet<string> DirectoriesToEmpty { get; private set; }
        public HashSet<string> FilesToRemove { get; private set; }

        public HashSet<string> DirectoriesToCreate { get; private set; }

        public Dictionary<string, UploadingFile> FilesToMove { get; private set; }

        public DetailedUpdateTask()
            {
            DirectoriesToEmpty = new HashSet<string>(new IgnoreCaseStringEqualityComparer());
            FilesToRemove = new HashSet<string>(new IgnoreCaseStringEqualityComparer());
            DirectoriesToCreate = new HashSet<string>(new IgnoreCaseStringEqualityComparer());
            FilesToMove = new Dictionary<string, UploadingFile>(new IgnoreCaseStringEqualityComparer());
            }
        }

    public static class Extentions
        {
        public static void AddAnyway<K>(this HashSet<K> hashSet, K value)
            {
            if (!hashSet.Contains(value))
                {
                hashSet.Add(value);
                }
            }

        public static void AddAnyway<K, V>(this Dictionary<K, V> dictionary, K key, V value)
            {
            if (!dictionary.ContainsKey(key))
                {
                dictionary.Add(key, value);
                }
            }
        }
    }
