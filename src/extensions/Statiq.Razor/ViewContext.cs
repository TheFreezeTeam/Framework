﻿using System.IO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Statiq.Common.Documents;
using Statiq.Common.Execution;

namespace Statiq.Razor
{
    internal class ViewContext : Microsoft.AspNetCore.Mvc.Rendering.ViewContext
    {
        public ViewContext(
            ActionContext actionContext,
            IView view,
            ViewDataDictionary viewData,
            ITempDataDictionary tempData,
            TextWriter writer,
            HtmlHelperOptions htmlHelperOptions,
            IDocument document,
            IExecutionContext executionContext)
            : base(actionContext, view, viewData, tempData, writer, htmlHelperOptions)
        {
            viewData[ViewDataKeys.StatiqDocument] = document;
            viewData[ViewDataKeys.StatiqExecutionContext] = executionContext;
        }
    }
}