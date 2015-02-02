// Copyright (c) Microsoft Open Technologies, Inc. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace Microsoft.AspNet.Mvc.Razor
{
    /// <summary>
    /// Specifies the contract for discovering precompiled views.
    /// </summary>
    public interface IPrecompiledViewsProvider
    {
        /// <summary>
        /// Gets a sequence of <see cref="PrecompiledViewInfo"/>.
        /// </summary>
        IEnumerable<PrecompiledViewInfo> PrecompiledViews { get; }
    }
}