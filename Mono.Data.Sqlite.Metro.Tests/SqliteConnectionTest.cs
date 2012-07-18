﻿// SqliteConnectionTest.cs - NUnit Test Cases for SqliteConnection
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
using Mono.Data.Sqlite;

using Microsoft.VisualStudio.TestPlatform.UnitTestFramework;
using TestFixtureAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestClassAttribute;
using SetUpAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestInitializeAttribute;
using TestAttribute = Microsoft.VisualStudio.TestPlatform.UnitTestFramework.TestMethodAttribute;
using System.IO;

namespace MonoTests.Mono.Data.Sqlite
{
    [TestFixture]
    public class SqliteConnectionTest
    {
        readonly static string dbRootPath = Windows.Storage.ApplicationData.Current.LocalFolder.Path;
        readonly static string _uri = Path.Combine(dbRootPath, "test.db");
        readonly static string _connectionString = "URI=file://" + _uri + ", version=3";
        SqliteConnection _conn = new SqliteConnection();

#if NET_2_0
                [Test]
                public void ConnectionStringTest_Null ()
                {
                    try
                    {
                        _conn.ConnectionString = null;
                        Assert.Fail();
                    }
                    catch (ArgumentNullException)
                    {
                    }
                    catch (Exception)
                    {
                        Assert.Fail();
                    }
                }

                [Test]
                public void ConnectionStringTest_MustBeClosed ()
                {
                        _conn.ConnectionString = _connectionString;
                        try {
                            _conn.Open();
                            try
                            {
                                _conn.ConnectionString = _connectionString;
                                Assert.Fail();
                            }
                            catch (InvalidOperationException)
                            {
                            }
                            catch (Exception)
                            {
                                Assert.Fail();
                            }
                    	} finally {
                    		_conn.Close ();
                    	}
                }

#else
        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConnectionStringTest_Empty()
        {
            _conn.ConnectionString = "";
            _conn.Open();
        }

        [Test]
        [ExpectedException(typeof(ArgumentException))]
        public void ConnectionStringTest_NoURI()
        {
            _conn.ConnectionString = "version=3";
            _conn.Open();
        }

        // In 2.0 _conn.Database always returns "main"
        [Test]
        public void ConnectionStringTest_IgnoreSpacesAndTrim()
        {
            _conn.ConnectionString = "URI=file://xyz      , ,,, ,, version=3";
            Assert.AreEqual("xyz", _conn.Database, "#1 file path is wrong");
        }
#endif
        // behavior has changed, I guess
        //[Test]
        // TODO [Ignore("opening a connection should not create db! though, leave for now")]
        public void OpenTest()
        {
            try
            {
                _conn.ConnectionString = _connectionString;
                _conn.Open();
                Assert.AreEqual(ConnectionState.Open, _conn.State, "#1 not opened");
                _conn.Close();

                // negative test: try opening a non-existent file
                _conn.ConnectionString = "URI=file://abcdefgh.db, version=3";
                try
                {
                    _conn.Open();
                    Assert.Fail("#1 should have failed on opening a non-existent db");
                }
                catch (ArgumentException e) { /*Console.WriteLine(e);*/ }

            }
            finally
            {
                if (_conn != null && _conn.State != ConnectionState.Closed)
                    _conn.Close();
            }
        }

    }
}