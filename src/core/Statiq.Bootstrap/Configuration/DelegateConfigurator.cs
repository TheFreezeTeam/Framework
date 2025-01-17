﻿using System;
using Statiq.Common.Configuration;

namespace Statiq.Bootstrap.Configuration
{
    public class DelegateConfigurator<TConfigurable> : IConfigurator<TConfigurable>
        where TConfigurable : IConfigurable
    {
        private readonly Action<TConfigurable> _action;

        public DelegateConfigurator(Action<TConfigurable> action)
        {
            _action = action;
        }

        public void Configure(TConfigurable configurable) => _action?.Invoke(configurable);
    }
}
