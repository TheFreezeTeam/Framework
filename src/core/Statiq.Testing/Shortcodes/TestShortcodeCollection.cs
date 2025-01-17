﻿using System;
using System.Collections.Generic;
using Statiq.Common.Shortcodes;

namespace Statiq.Testing.Shortcodes
{
    public class TestShortcodeCollection : Dictionary<string, Func<IShortcode>>, IShortcodeCollection
    {
        public TestShortcodeCollection()
            : base(StringComparer.OrdinalIgnoreCase)
        {
        }

        public IShortcode CreateInstance(string name) => this[name]();

        public bool Contains(string name) => ContainsKey(name);

        IEnumerator<string> IEnumerable<string>.GetEnumerator() => Keys.GetEnumerator();
    }
}
