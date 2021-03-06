// Copyright (c) 2011-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System.Collections.Generic;

namespace SiliconStudio.Xenko.Updater
{
    /// <summary>
    /// Implementation of <see cref="EnterChecker"/> for <see cref="IList{T}"/>.
    /// </summary>
    class ListEnterChecker<T> : EnterChecker
    {
        private readonly int minimumCount;

        public ListEnterChecker(int minimumCount)
        {
            this.minimumCount = minimumCount;
        }

        /// <inheritdoc/>
        public override bool CanEnter(object obj)
        {
            return minimumCount <= ((IList<T>)obj).Count;
        }
    }
}
