﻿using System;
using System.Runtime.InteropServices;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Statiq.Testing.Attributes
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    public class NonWindowsTestAttribute : Attribute, ITestAction
    {
        public ActionTargets Targets { get; }

        public void BeforeTest(ITest test)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Assert.Ignore("Non-Windows only");
            }
        }

        public void AfterTest(ITest test)
        {
        }
    }
}
