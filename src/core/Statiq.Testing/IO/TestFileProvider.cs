﻿using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ConcurrentCollections;
using Statiq.Common;
using Statiq.Common.IO;

namespace Statiq.Testing.IO
{
    public class TestFileProvider : IFileProvider
    {
        public TestFileProvider()
        {
        }

        public TestFileProvider(params string[] directories)
        {
            Directories.AddRange(directories);
        }

        public ICollection<string> Directories { get; } = new ConcurrentHashSet<string>();
        public ConcurrentDictionary<string, StringBuilder> Files { get; } = new ConcurrentDictionary<string, StringBuilder>();

        public Task<IDirectory> GetDirectoryAsync(DirectoryPath path) =>
            Task.FromResult<IDirectory>(new TestDirectory(this, path.FullPath));

        public Task<IFile> GetFileAsync(FilePath path) =>
            Task.FromResult<IFile>(new TestFile(this, path.FullPath));

        public void AddDirectory(string path) => Directories.Add(path);

        public void AddFile(string path, string content = "") => Files[path] = new StringBuilder(content);
    }
}
