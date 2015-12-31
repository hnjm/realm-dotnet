﻿/* Copyright 2015 Realm Inc - All Rights Reserved
 * Proprietary and Confidential
 */
 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.IO;
using NUnit.Framework;
using Realms;

namespace IntegrationTests
{
    [TestFixture]
    public class RealmInstanceTests
    {
        [Test]
        public void GetInstanceTest()
        {
            // Arrange, act and "assert" that no exception is thrown, using default location
            Realm.GetInstance();
        }

        [Test]
        public void InstanceIsClosedByDispose()
        {
            Realm temp;
            using (temp = Realm.GetInstance())
            {
                Assert.That(!temp.IsClosed);
            }
            Assert.That(temp.IsClosed);
        }

        [Test]
        public void GetInstanceWithJustFilenameTest()
        {
            // Arrange, act and "assert" that no exception is thrown, using default location
            Realm.GetInstance("EnterTheMagic.realm");
        }

        [Test]
        public void DeleteRealmWorksIfClosed()
        {
            // Arrange
            var config = new RealmConfiguration("EnterTheMagic.realm");
            var openRealm = Realm.GetInstance(config);

            // Act
            openRealm.Close();

            // Assert no error
            Assert.DoesNotThrow(() => Realm.DeleteRealm(config));
        }

        /*
         * uncomment when fix https://github.com/realm/realm-dotnet/issues/308
        [Test]
        public void DeleteRealmFailsIfOpenSameThread()
        {
            // Arrange
            var config = new RealmConfiguration("EnterTheMagic.realm");
            var openRealm = Realm.GetInstance(config);

            // Assert
            Assert.Throws<RealmPermissionDeniedException>(() => Realm.DeleteRealm(config));
        }
        */

        /*
        Comment out until work out how to fix
        see issue 199
        [Test]
        public void GetInstanceShouldThrowIfFileIsLocked()
        {
            // Arrange
            var databasePath = Path.GetTempFileName();
            using (File.Open(databasePath, FileMode.Open, FileAccess.Read, FileShare.None))     // Lock the file
            {
                // Act and assert
                Assert.Throws<RealmPermissionDeniedException>(() => Realm.GetInstance(databasePath));
            }
        }
        */

        [Test]
        public void GetInstanceShouldThrowWithBadPath()
        {
            // Arrange
            Assert.Throws<RealmPermissionDeniedException>(() => Realm.GetInstance("/"));
        }
    }

    [TestFixture]
    public class RealmConfigurationTests
    {

        [Test]
        public void DefaultConfigurationShouldHaveValidPath()
        {
            // Arrange
            var config = RealmConfiguration.DefaultConfiguration;

            // Assert
            Assert.That(Path.IsPathRooted(config.DatabasePath));
        }

        [Test]
        public void CanSetConfigurationPartialPath()
        {
            // Arrange
            var config = RealmConfiguration.DefaultConfiguration.ConfigWithPath("jan/docs/");

            // Assert
            Assert.That(Path.IsPathRooted(config.DatabasePath));
            Assert.That(config.DatabasePath, Is.StringEnding("/jan/docs/default.realm"));
        }
        
        [Test]
        public void PathIsCanonicalised()
        {
            // Arrange
            var config = RealmConfiguration.DefaultConfiguration.ConfigWithPath("../Documents/fred.realm");

            // Assert
            Assert.That(Path.IsPathRooted(config.DatabasePath));
            Assert.That(config.DatabasePath, Is.StringEnding("/Documents/fred.realm"));
            Assert.IsFalse(config.DatabasePath.Contains(".."));  // our use of relative up and down again was normalised out
        }

        [Test]
        public void CanOverrideConfigurationFilename()
        {
            // Arrange
            var config = new RealmConfiguration();
            var config2 = config.ConfigWithPath ("fred.realm");

            // Assert
            Assert.That(config2.DatabasePath, Is.StringEnding("fred.realm"));
        }

        [Test]
        public void CanSetDefaultConfiguration()
        {
            // Arrange
            var config = new RealmConfiguration();
            RealmConfiguration.DefaultConfiguration = config.ConfigWithPath ("fred.realm");

            // Assert
            Assert.That(RealmConfiguration.DefaultConfiguration.DatabasePath, Is.StringEnding("fred.realm"));
        }
    }

    [TestFixture]
    public class RealmObjectIntegrationTests
    {
        protected string _databasePath;
        protected Realm _realm;

        [SetUp]
        public void Setup()
        {
            _databasePath = Path.GetTempFileName();
            _realm = Realm.GetInstance(_databasePath);
        }

        [TearDown]
        public void TearDown()
        {
            _realm.Dispose();
        }


        void MakeThreePeople() 
        {
            Person p1, p2, p3;
            using (var transaction = _realm.BeginWrite ()) {
                p1 = _realm.CreateObject<Person> ();
                p1.FirstName = "John";
                p1.LastName = "Smith";
                p1.IsInteresting = true;
                p1.Email = "john@smith.com";
                p1.Score = -0.9907f;
                p1.Latitude = 51.508530;
                p1.Longitude = 0.076132;
                transaction.Commit ();
            }
            Debug.WriteLine ("p1 is named " + p1.FullName);

            using (var transaction = _realm.BeginWrite ()) {
                p2 = _realm.CreateObject<Person> ();
                p2.FullName = "John Doe";  // uses our setter whcih splits and maps to First/Lastname
                p2.IsInteresting = false;
                p2.Email = "john@deo.com";
                p2.Score = 100;
                p2.Latitude = 40.7637286;
                p2.Longitude = -73.9748113;
                transaction.Commit ();
            }
            Debug.WriteLine ("p2 is named " + p2.FullName);

            using (var transaction = _realm.BeginWrite ()) {
                p3 = _realm.CreateObject<Person> ();
                p3.FullName = "Peter Jameson";
                p3.Email = "peter@jameson.com";
                p3.IsInteresting = true;
                p3.Score = 42.42f;
                p3.Latitude = 37.7798657;
                p3.Longitude = -122.394179;
                transaction.Commit ();
            }
            Debug.WriteLine("p3 is named " + p3.FullName);
        }

        [Test, Explicit("Manual test for debugging")]
        public void SimpleTest()
        {
            MakeThreePeople ();
            var allPeople = _realm.All<Person>().ToList();
            Debug.WriteLine("There are " + allPeople.Count() + " in total");

            var interestingPeople = from p in _realm.All<Person>() where p.IsInteresting == true select p;

            Debug.WriteLine("Interesting people include:");
            foreach (var p in interestingPeople)
                Debug.WriteLine(" - " + p.FullName + " (" + p.Email + ")");

            var johns = from p in _realm.All<Person>() where p.FirstName == "John" select p;
            Debug.WriteLine("People named John:");
            foreach (var p in johns)
                Debug.WriteLine(" - " + p.FullName + " (" + p.Email + ")");
        }

        [Test]
        public void CreateObjectTest()
        {
            // Arrange and act
            using (var transaction = _realm.BeginWrite())
            {
                _realm.CreateObject<Person>();
                transaction.Commit(); 
            }

            // Assert
            var allPeople = _realm.All<Person>().ToList();
            Assert.That(allPeople.Count, Is.EqualTo(1));
        }

        [Test]
        public void ReadAndWriteEqualityTest()
        {
            // Arrange
            MakeThreePeople();
            var p1 = _realm.All<Person>().Where(p => p.Score >= 100).ToList()[0];
            var p2 = _realm.All<Person>().Where(p => p.Score >= 100).ToList()[0];
            Assert.That(p1.Equals(p2));

            // Act
            using (var transaction = _realm.BeginWrite())
            {
                p1.Score = 99.0f;
                Assert.That(p2.Score, Is.EqualTo(99.0f));  // value propagates despite transaction not finished
                Assert.That(p1.Equals(p2));  // identity-based comparison holds
                transaction.Commit(); 
            }

            // Assert
            Assert.That(p2.Score, Is.EqualTo(99.0f));  // value still holds after transaction finished
            Assert.That(p1.Equals(p2));  // identity-based comparison holds
        }

        [Test]
        public void SetAndGetPropertyTest()
        {
            // Arrange
            using (var transaction = _realm.BeginWrite())
            {
                Person p = _realm.CreateObject<Person>();

                // Act
                p.FirstName = "John";
                p.IsInteresting = true;
                p.Score = -0.9907f;
                p.Latitude = 51.508530;
                p.Longitude = 0.076132;
                transaction.Commit();
            }
            var allPeople = _realm.All<Person>().ToList();
            Person p2 = allPeople[0];  // pull it back out of the database otherwise can't tell if just a dumb property
            var receivedFirstName = p2.FirstName;
            var receivedIsInteresting = p2.IsInteresting;
            var receivedScore = p2.Score;
            var receivedLatitude = p2.Latitude;

            // Assert
            Assert.That(receivedFirstName, Is.EqualTo("John"));
            Assert.That(receivedIsInteresting, Is.True);
            Assert.That(receivedScore, Is.EqualTo(-0.9907f));
            Assert.That(receivedLatitude, Is.EqualTo(51.508530));
        }

        [Test]
        public void SetRemappedPropertyTest()
        {
            // Arrange
            Person p;
            using (var transaction = _realm.BeginWrite())
            {
                p = _realm.CreateObject<Person>();

                // Act
                p.Email = "John@a.com";

                transaction.Commit();
            }
            var receivedEmail = p.Email;

            // Assert
            Assert.That(receivedEmail, Is.EqualTo("John@a.com"));
        }

        [Test]
        public void CreateObjectOutsideTransactionShouldFail()
        {
            // Arrange, act and assert
            Assert.Throws<RealmOutsideTransactionException>(() => _realm.CreateObject<Person>());
        }

        [Test]
        public void ManageOutsideTransactionShouldFail()
        {
            var obj = new Person();
            Assert.Throws<RealmOutsideTransactionException>(() => _realm.Manage(obj));
        }

        [Test]
        public void ManageNullObjectShouldFail()
        {
            Assert.Throws<ArgumentNullException>(() => _realm.Manage(null as Person));
        }

        [Test]
        public void ManageAnObjectFromAnotherRealmShouldFail()
        {
            Person p;
            using (var transaction = _realm.BeginWrite())
            {
                p = _realm.CreateObject<Person>();
                transaction.Commit();
            }

            using (var otherRealm = Realm.GetInstance(Path.GetTempFileName()))
            {
                Assert.Throws<RealmObjectManagedByAnotherRealmException>(() => otherRealm.Manage(p));
            }
        }

        [Test]
        public void ManageAnObjectToRealmItAlreadyBelongsToShouldFail()
        {
            Person p;
            using (var transaction = _realm.BeginWrite())
            {
                p = _realm.CreateObject<Person>();
                transaction.Commit();
            }

            Assert.Throws<RealmObjectAlreadyManagedByRealmException>(() => _realm.Manage(p));
        }

        [Test]
        public void SetPropertyOutsideTransactionShouldFail()
        {
            // Arrange
            Person p;
            using (var transaction = _realm.BeginWrite())
            {
                p = _realm.CreateObject<Person>();
                transaction.Commit();
            }

            // Act and assert
            Assert.Throws<RealmOutsideTransactionException>(() => p.FirstName = "John");
        }


        [Test]
        public void RemoveSucceedsTest()
        {
            // Arrange
            Person p1, p2, p3;
            using (var transaction = _realm.BeginWrite())
            {
                //p1 = new Person { FirstName = "A" };
                //p2 = new Person { FirstName = "B" };
                //p3 = new Person { FirstName = "C" };
                p1 = _realm.CreateObject<Person>(); p1.FirstName = "A";
                p2 = _realm.CreateObject<Person>(); p2.FirstName = "B";
                p3 = _realm.CreateObject<Person>(); p3.FirstName = "C";
                transaction.Commit();
            }

            // Act
            using (var transaction = _realm.BeginWrite())
            {
                _realm.Remove(p2);
                transaction.Commit();
            }

            // Assert
            //Assert.That(!p2.InRealm);

            var allPeople = _realm.All<Person>().ToList();

            Assert.That(allPeople, Is.EquivalentTo(new List<Person> { p1, p3 }));
        }


        [Test]
        public void RemoveOutsideTransactionShouldFail()
        {
            // Arrange
            Person p;
            using (var transaction = _realm.BeginWrite())
            {
                p = _realm.CreateObject<Person>();
                transaction.Commit();
            }

            // Act and assert
            Assert.Throws<RealmOutsideTransactionException>(() => _realm.Remove(p) );
        }


        // Extension method rather than SQL-style LINQ
        [Test]
        public void SearchComparingFloat()
        {
            MakeThreePeople (); 
            var s0 = _realm.All<Person>().Where(p => p.Score == 42.42f).ToList();
            Assert.That(s0.Count, Is.EqualTo(1));
            Assert.That(s0[0].Score, Is.EqualTo(42.42f));

            var s1 = _realm.All<Person>().Where(p => p.Score != 100.0f).ToList();
            Assert.That(s1.Count, Is.EqualTo(2));
            Assert.That(s1[0].Score, Is.EqualTo(-0.9907f));
            Assert.That(s1[1].Score, Is.EqualTo(42.42f));

            var s2 = _realm.All<Person>().Where(p => p.Score < 0).ToList();
            Assert.That(s2.Count, Is.EqualTo(1));
            Assert.That(s2[0].Score, Is.EqualTo(-0.9907f));

            var s3 = _realm.All<Person>().Where(p => p.Score <= 42.42f).ToList();
            Assert.That(s3.Count, Is.EqualTo(2));
            Assert.That(s3[0].Score, Is.EqualTo(-0.9907f));
            Assert.That(s3[1].Score, Is.EqualTo(42.42f));

            var s4 = _realm.All<Person>().Where(p => p.Score > 99.0f).ToList();
            Assert.That(s4.Count, Is.EqualTo(1));
            Assert.That(s4[0].Score, Is.EqualTo(100.0f));

            var s5 = _realm.All<Person>().Where(p => p.Score >= 100).ToList();
            Assert.That(s5.Count, Is.EqualTo(1));
            Assert.That(s5[0].Score, Is.EqualTo(100.0f));
        }

        [Test]
        public void SearchComparingDouble()
        {
            MakeThreePeople (); 
            var s0 = _realm.All<Person>().Where(p => p.Latitude == 40.7637286).ToList();
            Assert.That(s0.Count, Is.EqualTo(1));
            Assert.That(s0[0].Latitude, Is.EqualTo(40.7637286));

            var s1 = _realm.All<Person>().Where(p => p.Latitude != 40.7637286).ToList();
            Assert.That(s1.Count, Is.EqualTo(2));
            Assert.That(s1[0].Latitude, Is.EqualTo(51.508530));
            Assert.That(s1[1].Latitude, Is.EqualTo(37.7798657));

            var s2 = _realm.All<Person>().Where(p => p.Latitude < 40).ToList();
            Assert.That(s2.Count, Is.EqualTo(1));
            Assert.That(s2[0].Latitude, Is.EqualTo(37.7798657));

            var s3 = _realm.All<Person>().Where(p => p.Latitude <= 40.7637286).ToList();
            Assert.That(s3.Count, Is.EqualTo(2));
            Assert.That(s3[0].Latitude, Is.EqualTo(40.7637286));
            Assert.That(s3[1].Latitude, Is.EqualTo(37.7798657));

            var s4 = _realm.All<Person>().Where(p => p.Latitude > 50).ToList();
            Assert.That(s4.Count, Is.EqualTo(1));
            Assert.That(s4[0].Latitude, Is.EqualTo(51.508530));

            var s5 = _realm.All<Person>().Where(p => p.Latitude >= 51.508530).ToList();
            Assert.That(s5.Count, Is.EqualTo(1));
            Assert.That(s5[0].Latitude, Is.EqualTo(51.508530));
        }

        [Test]
        public void NonAutomaticPropertiesShouldNotBeWoven()
        {
            Assert.That(typeof(Person).GetProperty("Nickname").GetCustomAttributes(typeof(WovenPropertyAttribute), false), Is.Empty);
        }

        [Test]
        public void NonAutomaticPropertiesShouldBeIgnored()
        {
            using (var trans = _realm.BeginWrite())
            {
                var p = _realm.CreateObject<Person>();
                p.FirstName = "Vincent";
                p.LastName = "Adultman";
                p.Nickname = "Vinnie";
                trans.Commit();
            }

            var vinnie = _realm.All<Person>().ToList().Single();
            Assert.That(vinnie.FullName, Is.EqualTo("Vincent Adultman"));
            Assert.That(string.IsNullOrEmpty(vinnie.Nickname));
        }
    }

    [TestFixture]
    public class RealmMigrationTests
    {
        [Test]
        public void TriggerMigrationBySchemaVersion()
        {
            // Arrange
            var config1 = new RealmConfiguration("ForMigrations.realm");
            Realm.DeleteRealm(config1);  // ensure start clean
            var realm1 = Realm.GetInstance(config1);
            // new database doesn't push back a version number
            Assert.That(config1.SchemaVersion, Is.EqualTo(RealmConfiguration.NotVersioned));
            realm1.Close();

            // Act
            var config2 = config1.ConfigWithPath("ForMigrations.realm");
            config2.SchemaVersion = 99;
            Realm realm2 = null;  // should be updated by DoesNotThrow

            // Assert
            Assert.DoesNotThrow( () => realm2 = Realm.GetInstance(config2) ); // same path, different version, should auto-migrate quietly
            Assert.That(realm2.Config.SchemaVersion, Is.EqualTo(99));

        }


        [Test, Explicit("Manual test after changing Person schema")]
        public void TriggerMigrationBySchemaEditing()
        {
            // NOTE go edit the schema in Person.cs and comment/uncomment ExtraToTriggerMigration
            // Arrange
            var config1 = new RealmConfiguration("ForMigrationsBySchema.realm");
            var realm1 = Realm.GetInstance(config1);
        }
    }
}
