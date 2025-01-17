﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Statiq.Common.IO
{
    /// <summary>
    /// An ordered collection of unique <see cref="NormalizedPath"/>.
    /// </summary>
    /// <typeparam name="TPath">The type of the path (file or directory).</typeparam>
    public class PathCollection<TPath> : IReadOnlyList<TPath>
        where TPath : NormalizedPath
    {
        private static readonly PathEqualityComparer _comparer = new PathEqualityComparer();

        private readonly object _pathsLock = new object();
        private readonly List<TPath> _paths = new List<TPath>();

        /// <summary>
        /// Initializes a new path collection.
        /// </summary>
        public PathCollection()
        {
        }

        /// <summary>
        /// Initializes a new path collection.
        /// </summary>
        /// <param name="paths">The paths.</param>
        public PathCollection(IEnumerable<TPath> paths)
        {
            AddRange(paths);
        }

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns>
        /// An <c>IEnumerator&lt;TPath&gt;</c> that can be used to iterate through the collection.
        /// </returns>
        public IEnumerator<TPath> GetEnumerator()
        {
            lock (_pathsLock)
            {
                // Copy to a new list for consumers so as not to lock during enumeration
                return _paths.ToList().GetEnumerator();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Gets the number of directories in the collection.
        /// </summary>
        /// <value>The number of directories in the collection.</value>
        public int Count
        {
            get
            {
                lock (_pathsLock)
                {
                    return _paths.Count;
                }
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="Statiq.Common.IO.DirectoryPath" /> at the specified index.
        /// </summary>
        /// <value>
        /// The <see cref="Statiq.Common.IO.DirectoryPath" /> at the specified index.
        /// </value>
        /// <param name="index">The index.</param>
        /// <returns>The path at the specified index.</returns>
        public TPath this[int index]
        {
            get
            {
                lock (_pathsLock)
                {
                    return _paths[index];
                }
            }
            set
            {
                _ = value ?? throw new ArgumentNullException(nameof(value));

                lock (_pathsLock)
                {
                    _paths[index] = value;
                }
            }
        }

        /// <summary>
        /// Adds the specified path to the collection.
        /// </summary>
        /// <param name="path">The path to add.</param>
        /// <returns>
        /// <c>true</c> if the path was added; <c>false</c> if the path was already present.
        /// </returns>
        public bool Add(TPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            lock (_pathsLock)
            {
                if (_paths.Contains(path, new PathEqualityComparer()))
                {
                    return false;
                }
                _paths.Add(path);
                return true;
            }
        }

        /// <summary>
        /// Adds the specified paths to the collection.
        /// </summary>
        /// <param name="paths">The paths to add.</param>
        public void AddRange(IEnumerable<TPath> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            lock (_pathsLock)
            {
                foreach (TPath path in paths)
                {
                    if (!_paths.Contains(path, _comparer))
                    {
                        _paths.Add(path);
                    }
                }
            }
        }

        /// <summary>
        /// Clears all paths from the collection.
        /// </summary>
        public void Clear()
        {
            lock (_pathsLock)
            {
                _paths.Clear();
            }
        }

        /// <summary>
        /// Determines whether the collection contains the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns><c>true</c> if the collection contains the path, otherwise <c>false</c>.</returns>
        public bool Contains(TPath path)
        {
            lock (_pathsLock)
            {
                return _paths.Contains(path, _comparer);
            }
        }

        /// <summary>
        /// Removes the specified path.
        /// </summary>
        /// <param name="path">The path to remove.</param>
        /// <returns><c>true</c> if the collection contained the path, otherwise <c>false</c>.</returns>
        public bool Remove(TPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            lock (_pathsLock)
            {
                int index = _paths.FindIndex(x => x.Equals(path));
                if (index == -1)
                {
                    return false;
                }
                _paths.RemoveAt(index);
                return true;
            }
        }

        /// <summary>
        /// Removes the specified paths from the collection.
        /// </summary>
        /// <param name="paths">The paths to remove.</param>
        public void RemoveRange(IEnumerable<TPath> paths)
        {
            if (paths == null)
            {
                throw new ArgumentNullException(nameof(paths));
            }

            lock (_pathsLock)
            {
                foreach (TPath path in paths)
                {
                    int index = _paths.FindIndex(x => x.Equals(path));
                    if (index != -1)
                    {
                        _paths.RemoveAt(index);
                    }
                }
            }
        }

        /// <summary>
        /// Returns the index of the specified path.
        /// </summary>
        /// <param name="path">The path.</param>
        /// <returns>The index of the specified path, or -1 if not found.</returns>
        public int IndexOf(TPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            lock (_pathsLock)
            {
                return _paths.FindIndex(x => x.Equals(path));
            }
        }

        /// <summary>
        /// Inserts the path at the specified index.
        /// </summary>
        /// <param name="index">The index where the path should be inserted.</param>
        /// <param name="path">The path to insert.</param>
        /// <returns><c>true</c> if the collection did not contain the path and it was inserted, otherwise <c>false</c></returns>
        public bool Insert(int index, TPath path)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            lock (_pathsLock)
            {
                if (_paths.Contains(path, _comparer))
                {
                    return false;
                }
                _paths.Insert(index, path);
                return true;
            }
        }

        /// <summary>
        /// Removes the path at the specified index.
        /// </summary>
        /// <param name="index">The index where the path should be removed.</param>
        public void RemoveAt(int index)
        {
            lock (_pathsLock)
            {
                _paths.RemoveAt(index);
            }
        }
    }
}
