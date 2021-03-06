// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using JetBrains.Annotations;
using Microsoft.Data.Entity.ChangeTracking.Internal;
using Microsoft.Data.Entity.Storage;
using Remotion.Linq.Clauses;

namespace Microsoft.Data.Entity.Query.ExpressionVisitors.Internal
{
    public abstract class EntityShaper : Shaper
    {
        protected EntityShaper(
            [NotNull] IQuerySource querySource,
            [NotNull] string entityType,
            [NotNull] KeyValueFactory keyValueFactory,
            [NotNull] Func<ValueBuffer, object> materializer)
            : base(querySource)
        {
            EntityType = entityType;
            KeyValueFactory = keyValueFactory;
            Materializer = materializer;
        }

        protected virtual string EntityType { get; }
        protected virtual KeyValueFactory KeyValueFactory { get; }
        protected virtual Func<ValueBuffer, object> Materializer { get; }
        protected virtual bool AllowNullResult { get; private set; }
        protected virtual int ValueBufferOffset { get; private set; }

        public virtual EntityShaper WithOffset(int offset)
        {
            ValueBufferOffset += offset;
            AllowNullResult = true;

            return this;
        }
    }
}
