﻿using System;
using JavaScriptEngineSwitcher.Core;
using JavaScriptEngineSwitcher.Jint;
using JSPool;
using Statiq.Common.JavaScript;
using IJavaScriptEngine = Statiq.Common.JavaScript.IJavaScriptEngine;

namespace Statiq.Core.JavaScript
{
    internal class JavaScriptEnginePool : IJavaScriptEnginePool
    {
        private readonly JsPool<JavaScriptEngine> _pool;
        private bool _disposed = false;

        public JavaScriptEnginePool(
            Action<IJavaScriptEngine> initializer,
            int startEngines,
            int maxEngines,
            int maxUsagesPerEngine,
            TimeSpan engineTimeout)
        {
            // First we need to check if the JsEngineSwitcher has been configured. We'll do this
            // by checking the DefaultEngineName being set. If that's there we can safely assume
            // its been configured somehow (maybe via a configuration file). If not we'll wire up
            // Jint as the default engine.
            if (string.IsNullOrWhiteSpace(JsEngineSwitcher.Current.DefaultEngineName))
            {
                JsEngineSwitcher.Current.EngineFactories.Add(new JintJsEngineFactory());
                JsEngineSwitcher.Current.DefaultEngineName = JintJsEngine.EngineName;
            }

            _pool = new JsPool<JavaScriptEngine>(new JsPoolConfig<JavaScriptEngine>
            {
                EngineFactory = () => new JavaScriptEngine(JsEngineSwitcher.Current.CreateDefaultEngine()),
                Initializer = x => initializer?.Invoke(x),
                StartEngines = startEngines,
                MaxEngines = maxEngines,
                MaxUsagesPerEngine = maxUsagesPerEngine,
                GetEngineTimeout = engineTimeout
            });
        }

        public void Dispose()
        {
            CheckDisposed();
            _pool.Dispose();
            _disposed = true;
        }

        public IJavaScriptEngine GetEngine(TimeSpan? timeout = null) => new PooledJavaScriptEngine(_pool.GetEngine(timeout), _pool);

        public void RecycleEngine(IJavaScriptEngine engine)
        {
            if (engine == null)
            {
                throw new ArgumentNullException(nameof(engine));
            }
            PooledJavaScriptEngine pooledEngine = engine as PooledJavaScriptEngine;
            if (pooledEngine == null)
            {
                throw new ArgumentException("The specified engine was not from a pool");
            }
            if (pooledEngine.Pool != _pool)
            {
                throw new ArgumentException("The specified engine is from a different pool");
            }
            _pool.DisposeEngine(pooledEngine.Engine);
        }

        public void RecycleAllEngines() => _pool.Recycle();

        private void CheckDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(JsPool));
            }
        }
    }
}
