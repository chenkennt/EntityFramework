﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Data.Entity.Infrastructure;

namespace Microsoft.Data.Entity.Sqlite.FunctionalTests
{
    public class QueryNoClientEvalSqliteFixture : NorthwindQuerySqliteFixture
    {
        protected override void ConfigureOptions(SqliteDbContextOptionsBuilder sqlServerDbContextOptionsBuilder)
            => sqlServerDbContextOptionsBuilder.DisableQueryClientEvaluation();
    }
}
