// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Data;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using Dapper;
using NUnit.Framework;
using Microsoft.Data.Sqlite.Utilities;
using static Microsoft.Data.Sqlite.TestUtilities.Constants;
using Dapper;

namespace Microsoft.Data.Sqlite {
    [TestFixture]
    public class SqlitePerformanceTest {
        [SetUp]
        public void Init() {
            var bs = Bootstrap.Bootstrapper.Instance;
        }


        [Test]
        public void InsertSpeed() {
            var connectionString = "Data Source=perf_test.db;";

            using (var connection = new SqliteConnection(connectionString)) {
                connection.Open();
                connection.ExecuteNonQuery("PRAGMA main.page_size = 4096;");
                connection.ExecuteNonQuery("PRAGMA main.cache_size = 10000;");
                connection.ExecuteNonQuery("PRAGMA synchronous = OFF;"); // NORMAL or OFF 20% faster
                connection.ExecuteNonQuery("PRAGMA journal_mode = WAL;");
                connection.ExecuteNonQuery("PRAGMA main.cache_size = 5000;");
                connection.ExecuteNonQuery("CREATE TABLE IF NOT EXISTS Numbers (Key INTEGER, Value REAL);");


                var sw = new Stopwatch();
                sw.Start();
                for (int i = 200000; i < 300000; i++) {
                    //connection.ExecuteNonQuery($"INSERT INTO Numbers VALUES ({i}, {i});");
                    connection.Execute("INSERT INTO Numbers VALUES (@Key, @Value);",
                        new { Key = (long)i, Value = (double)i });
                }
                sw.Stop();
                Console.WriteLine($"Elapsed, msec {sw.ElapsedMilliseconds}");



            }
        }


    }
}