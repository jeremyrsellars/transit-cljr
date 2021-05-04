﻿// Copyright © 2014 Rick Beerendonk. All Rights Reserved.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS-IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using NUnit.Framework;

namespace Beerendonk.Transit.Java.Tests
{
    [TestFixture]
    public class ConvertTest
    {
        [Test]
        public void TestFromJavaTime()
        {
            var expected = new DateTimeOffset(new DateTime(2014, 8, 15, 13, 25, 37, 481, DateTimeKind.Utc)).LocalDateTime;
            var javaTime = 1408109137481L;
            DateTime result = Java.Convert.FromJavaTimeToLocal(javaTime);
            Assert.AreEqual(expected, result);
            Assert.AreEqual(DateTimeKind.Local, result.Kind);
        }

        [Test]
        public void TestFromJavaTimeToUtc()
        {
            var expected = new DateTime(2014, 8, 15, 13, 25, 37, 481, DateTimeKind.Utc);
            var javaTime = 1408109137481L;
            DateTime result = Java.Convert.FromJavaTimeToUtc(javaTime);
            Assert.AreEqual(expected, result);
            Assert.AreEqual(DateTimeKind.Utc, result.Kind);
        }

        [Test]
        public void TestLocalToJavaTime()
        {
            var expected = 1407575935427L;
            var dateTime = new DateTimeOffset(new DateTime(2014, 8, 9, 9, 18, 55, 427, DateTimeKind.Utc)).LocalDateTime;
            var result = Java.Convert.ToJavaTime(dateTime);
            Assert.AreEqual(expected, result);
        }

        [Test]
        public void TestUtcToJavaTime()
        {
            var expected = 1407575935427L;
            var dateTime = new DateTime(2014, 8, 9, 9, 18, 55, 427, DateTimeKind.Utc);
            var result = Java.Convert.ToJavaTime(dateTime);
            Assert.AreEqual(expected, result);
        }
    }
}
