﻿// SqliteDataReaderTest.cs - NUnit Test Cases for SqliteDataReader
//
// Authors:
//   Sureshkumar T <tsureshkumar@novell.com>
// 

//
// Copyright (C) 2004 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
// 
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//


using System;
using System.Data;
using System.IO;
using Mono.Data.Sqlite;

#if SILVERLIGHT && !WINDOWS_PHONE
using Microsoft.VisualStudio.TestTools.UnitTesting;
#else
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
#endif

namespace MonoTests.Mono.Data.Sqlite
{
    [TestClass]
    public class SqliteDataReaderTest
    {
        readonly static string dbRootPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        readonly static string _uri = Path.Combine(dbRootPath, "test.db");
        readonly static string _connectionString = "URI=file://" + _uri + ", version=3";
        static SqliteConnection _conn = new SqliteConnection();

        [ClassInitialize]
#if !SILVERLIGHT || WINDOWS_PHONE
        static public void FixtureSetUp(TestContext context)
#else
        public void FixtureSetUp()
#endif
        {
            //if (!File.Exists(_uri) || new FileInfo(_uri).Length == 0)
            {
                // ignore all tests
                // Assert.Ignore("#000 ignoring all fixtures. No database present");

                _conn.ConnectionString = _connectionString;
                using (_conn)
                {
                    _conn.Open();
                    SqliteCommand cmd = _conn.CreateCommand();
                    cmd.CommandText = @"
create table test (
  id int NOT NULL PRIMARY KEY,
  name varchar (20)
);

insert into test values (1, ""mono test 1"");
insert into test values (2, ""mono test 2"");
insert into test values (3, ""mono test 3"");
";
                    cmd.ExecuteNonQuery();
                    _conn.Close();
                }
            }
        }

        [TestMethod]
        public void TypeOfNullInResultTest()
        {
            _conn.ConnectionString = _connectionString;
            SqliteDataReader reader = null;
            using (_conn)
            {
                _conn.Open();
                SqliteCommand cmd = (SqliteCommand)_conn.CreateCommand();
                cmd.CommandText = "select null from test";
                reader = cmd.ExecuteReader();
                try
                {
                    Assert.IsTrue(reader.Read());
                    Assert.IsNotNull(reader.GetFieldType(0));
                }
                finally
                {
                    if (reader != null && !reader.IsClosed)
                        reader.Close();
                    _conn.Close();
                }
            }
        }

        [TestMethod]
        public void TimestampTest()
        {
            _conn.ConnectionString = _connectionString;
            using (_conn)
            {
                _conn.Open();
                var cmd = (SqliteCommand)_conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS TestNullableDateTime (nullable TIMESTAMP NULL, dummy int); INSERT INTO TestNullableDateTime (nullable, dummy) VALUES (124123, 2);";
                cmd.ExecuteNonQuery();

                var query = "SELECT * FROM TestNullableDateTime;";
                cmd = (SqliteCommand)_conn.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    try
                    {
                        var dt = reader["nullable"];
                        Assert.Fail("Expected: FormatException");
                    }
                    catch (FormatException)
                    {
                        // expected this one
                    }
                }
            }

            _conn.ConnectionString = _connectionString + ",DateTimeFormat=UnixEpoch";
            using (_conn)
            {
                _conn.Open();
                var cmd = (SqliteCommand)_conn.CreateCommand();
                cmd.CommandText = @"CREATE TABLE IF NOT EXISTS TestNullableDateTime (nullable TIMESTAMP NULL, dummy int); INSERT INTO TestNullableDateTime (nullable, dummy) VALUES (124123, 2);";
                cmd.ExecuteNonQuery();

                var query = "SELECT * FROM TestNullableDateTime;";
                cmd = (SqliteCommand)_conn.CreateCommand();
                cmd.CommandText = query;
                cmd.CommandType = CommandType.Text;

                using (var reader = cmd.ExecuteReader())
                {
                    // this should succeed now
                    var dt = reader["nullable"];
                }
            }
        }

        [TestMethod]
        public void CloseConnectionTest()
        {
            // When this test fails it may confuse nunit a bit, causing it to show strange
            // exceptions, since it leaks file handles (and nunit tries to open files,
            // which it doesn't expect to fail).

            // For the same reason a lot of other tests will fail when this one fails.

            _conn.ConnectionString = _connectionString;
            using (_conn)
            {
                _conn.Open();
                using (var cmd = (SqliteCommand)_conn.CreateCommand())
                {
                    cmd.CommandText = @"CREATE TABLE IF NOT EXISTS TestNullableDateTime (nullable TIMESTAMP NULL, dummy int); INSERT INTO TestNullableDateTime (nullable, dummy) VALUES (124123, 2);";
                    cmd.ExecuteNonQuery();
                }
            }

            for (int i = 0; i < 1000; i++)
            {
                _conn.ConnectionString = _connectionString;
                using (_conn)
                {
                    _conn.Open();
                    using (var cmd = (SqliteCommand)_conn.CreateCommand())
                    {
                        cmd.CommandText = "SELECT * FROM TestNullableDateTime;";
                        cmd.CommandType = CommandType.Text;

                        using (var reader = cmd.ExecuteReader(CommandBehavior.CloseConnection))
                        {
                            reader.Read();
                        }
                    }
                }
            }
        }

        [TestMethod]
        public void TestDataTypes()
        {
            _conn.ConnectionString = _connectionString;
            using (_conn)
            {
                _conn.Open();

                using (var cm = _conn.CreateCommand())
                {
                    cm.CommandText = "DROP TABLE TEST; CREATE TABLE TEST (F2 DATETIME, F3 guidblob NOT NULL); INSERT INTO TEST (F2, F3) VALUES (:F2, :F3)";

                    var dp2 = cm.CreateParameter();
                    dp2.ParameterName = ":F2";
                    dp2.Value = DateTime.Now;
                    cm.Parameters.Add(dp2);
                    var dp3 = cm.CreateParameter();
                    dp3.ParameterName = ":F3";
                    dp3.Value = new byte[] { 3, 14, 15 };
                    cm.Parameters.Add(dp3);

                    cm.ExecuteNonQuery();
                }

                using (var cm = _conn.CreateCommand())
                {
                    cm.CommandText = "SELECT * FROM TEST";
                    using (var dr = cm.ExecuteReader())
                    {
                        dr.Read();

                        Assert.AreEqual("System.DateTime", dr.GetFieldType(dr.GetOrdinal("F2")).ToString(), "F2");
                        Assert.AreEqual("guidblob", dr.GetDataTypeName(dr.GetOrdinal("F3")), "F3");
                        Assert.AreEqual("System.Guid", dr.GetFieldType(dr.GetOrdinal("F3")).ToString(), "F3-#2");
                    }
                }
            }
        }
    }
}