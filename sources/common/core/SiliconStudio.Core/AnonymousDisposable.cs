// Copyright (c) 2014-2017 Silicon Studio Corp. All rights reserved. (https://www.siliconstudio.co.jp)
// See LICENSE.md for full license information.
using System;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Core
{
    /// <summary>
    /// This class allows implementation of <see cref="IDisposable"/> using anonymous functions.
    /// The anonymous function will be invoked only on the first call to the <see cref="Dispose"/> method.
    /// </summary>
    public sealed class AnonymousDisposable : IDisposable
    {
        private bool isDisposed;
        private Action onDispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="AnonymousDisposable"/> class.
        /// </summary>
        /// <param name="onDispose">The anonymous function to invoke when this object is disposed.</param>
        public AnonymousDisposable([NotNull] Action onDispose)
        {
            if (onDispose == null) throw new ArgumentNullException(nameof(onDispose));

            this.onDispose = onDispose;
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            if (isDisposed)
                return;

            isDisposed = true;

            onDispose();
            onDispose = null;
        }
    }
}
