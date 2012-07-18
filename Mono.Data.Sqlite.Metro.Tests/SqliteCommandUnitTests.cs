﻿// SqliteDataAdapterUnitTests.cs - NUnit Test Cases for Mono.Data.Sqlite.SqliteDataAdapter
//
// Author(s):	Thomas Zoechling <thomas.zoechling@gmx.at>


using System;
using System.Data;
using System.IO;
using System.Text;
using System.Linq;
using Mono.Data.Sqlite;
using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using SetUpAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.ClassInitializeAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;

namespace MonoTests.Mono.Data.Sqlite
{

    [TestFixture]
    public class SqliteCommandUnitTests
    {
        readonly static string dbRootPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        readonly static string _uri = Path.Combine(dbRootPath, "SqliteTest.db");

        readonly static string _connectionString = "URI=file://" + _uri + ", version=3";
        static SqliteConnection _conn = new SqliteConnection(_connectionString);
        readonly static string stringvalue = "my keyboard is better than yours : äöüß";

        public SqliteCommandUnitTests()
        {
        }

        [SetUp]
        static public void Create(TestContext context)
        {
            System.Diagnostics.Debug.WriteLine("E Y A!");
            try
            {
                try
                {
                    var file = Windows.Storage.ApplicationData.Current.LocalFolder.GetFileAsync(_uri).AsTask();
                    file.Wait();
                    _conn.Dispose();
                    // We want to start with a fresh db for each full run
                    // The database is created on the first open()
                    file.Result.DeleteAsync().AsTask().Wait();
                }
                catch (AggregateException ex)
                {
                    if (!(ex.InnerExceptions.Single() is FileNotFoundException))
                    {
                        throw;
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }

            try
            {
                using (SqliteCommand createCommand = new SqliteCommand("CREATE TABLE t1(t  TEXT,  f FLOAT, i INTEGER, b TEXT);", _conn))
                using (SqliteCommand insertCommand = new SqliteCommand("INSERT INTO t1  (t, f, i, b ) VALUES('" + stringvalue + "',123,123,'123')", _conn))
                {
                    System.Diagnostics.Debug.WriteLine("Before");
                    _conn.Open();
                    System.Diagnostics.Debug.WriteLine("AFTER");
                    createCommand.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("EXEC");
                    insertCommand.ExecuteNonQuery();
                }
            }
            catch (Exception e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                //Console.WriteLine(e);
                throw new AssertFailedException("Create table failed", e);
            }
            finally
            {
                _conn.Close();
                System.Diagnostics.Debug.WriteLine("FINISHED SETUP");
            }
        }

        [Test]
        public void Select()
        {
            System.Diagnostics.Debug.WriteLine(_connectionString);
            using (_conn)
            using (SqliteCommand simpleSelect = new SqliteCommand("SELECT * FROM t1;  ", _conn)) // check trailing spaces
            {
                _conn.Open();
                using (SqliteDataReader dr = simpleSelect.ExecuteReader())
                {
                    while (dr.Read())
                    {
                        string test = dr[0].ToString();
                        Assert.AreEqual(dr["T"], stringvalue); // also checks case-insensitive column
                        Assert.AreEqual(dr["F"], 123D);
                        Assert.AreEqual(dr["I"], 123L);
                        Assert.AreEqual(dr["B"], "123");
                    }
                    Assert.IsTrue(dr.FieldCount > 0);
                }
            }
        }

        [Test]
        public void Delete()
        {
            using (_conn)
            using (SqliteCommand insCmd = new SqliteCommand("INSERT INTO t1 VALUES ('todelete',0.1,0,'')", _conn))
            using (SqliteCommand delCmd = new SqliteCommand("DELETE FROM t1 WHERE t = 'todelete'", _conn))
            {
                _conn.Open();
                int insReturn = insCmd.ExecuteNonQuery();
                int delReturn = delCmd.ExecuteNonQuery();

                Assert.IsTrue(insReturn == delReturn);
            }
        }

        [Test]
        public void Insert()
        {
            using (_conn)
            using (SqliteCommand insCmd = new SqliteCommand("INSERT INTO t1 VALUES ('inserted',0.1,0,'')", _conn))
            {
                _conn.Open();
                int insReturn = insCmd.ExecuteNonQuery();
                Assert.IsTrue(insReturn == 1);
            }
        }

        [Test]
        public void Update()
        {
            using (_conn)
            using (SqliteCommand insCmd = new SqliteCommand("INSERT INTO t1 VALUES ('toupdate',0.1,0,'')", _conn))
            using (SqliteCommand updCmd = new SqliteCommand("UPDATE t1 SET t = 'updated' ,f = 2.0, i = 2, b = '' WHERE t = 'toupdate'", _conn))
            {
                _conn.Open();
                insCmd.ExecuteNonQuery();
                Assert.IsTrue(updCmd.ExecuteNonQuery() == 1);
            }
        }


        [Test]
        public void ScalarReturn()
        {
            // This should return the 1 line that got inserted in CreateTable() Test
            using (_conn)
            using (SqliteCommand cmd = new SqliteCommand("SELECT COUNT(*) FROM t1 WHERE  t LIKE '%äöüß'", _conn))
            {
                _conn.Open();
                Assert.AreEqual(1, Convert.ToInt32(cmd.ExecuteScalar()));
            }
        }

        [Test]
        public void InsertWithTransaction()
        {
            _conn.Open();
            using (_conn)
            using (SqliteTransaction t = _conn.BeginTransaction() as SqliteTransaction)
            using (SqliteCommand c1 = new SqliteCommand("INSERT INTO t1 VALUES ('a',0.1,0,'0')", _conn, t))
            using (SqliteCommand c2 = new SqliteCommand("INSERT INTO t1 VALUES ('b',1.2,0,'0')", _conn, t))
            using (SqliteCommand c3 = new SqliteCommand("INSERT INTO t1 VALUES ('c',0.3,1,'0')", _conn, t))
            using (SqliteCommand c4 = new SqliteCommand("INSERT INTO t1 VALUES ('d',0.4,0,'1')", _conn, t))
            {
                try
                {
                    c1.ExecuteNonQuery();
                    c2.ExecuteNonQuery();
                    c3.ExecuteNonQuery();
                    c4.ExecuteNonQuery();
                    t.Commit();
                }
                catch (Exception e)
                {
                    t.Rollback();
                    throw new AssertFailedException("Sqlite Commands failed", e);
                }
            }
        }

        [Test]
        public void InsertWithFailingTransaction()
        {
            _conn.Open();
            using (_conn)
            using (SqliteTransaction t = _conn.BeginTransaction() as SqliteTransaction)
            using (SqliteCommand c1 = new SqliteCommand("INSERT INTO t1 VALUES ('1','0','0','0')", _conn, t))
            using (SqliteCommand c2 = new SqliteCommand("INSERT INTO t1 VALUES ('0','1','0','0')", _conn, t))
            using (SqliteCommand c3 = new SqliteCommand("INSERT INTO t1 VALUES ('x',?,'x',?,'x',?,'x')", _conn, t))
            using (SqliteCommand c4 = new SqliteCommand("INSERT INTO t1 VALUES ('0','0','0','1')", _conn, t))
            {
                try
                {
                    c1.ExecuteNonQuery();
                    c2.ExecuteNonQuery();
                    c3.ExecuteNonQuery();
                    c4.ExecuteNonQuery();
                    t.Commit();
                    Assert.Fail();
                }
                catch (SqliteException)
                {
                }
                catch (Exception)
                {
                    Assert.Fail();
                }
                finally
                {
                    t.Rollback();
                }
            }
        }
    }
}