/*
 * Copyright (C) 2010 The Libphonenumber Authors
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

namespace com.google.i18n.phonenumbers {

using PhoneNumberType = com.google.i18n.phonenumbers.PhoneNumberUtil.PhoneNumberType;
using PhoneNumber = com.google.i18n.phonenumbers.Phonenumber.PhoneNumber;
using NumberFormat = com.google.i18n.phonenumbers.Phonemetadata.NumberFormat;
using PhoneMetadata = com.google.i18n.phonenumbers.Phonemetadata.PhoneMetadata;
using CountryCodeSource = com.google.i18n.phonenumbers.Phonenumber.PhoneNumber.CountryCodeSource;
using MatchType = com.google.i18n.phonenumbers.PhoneNumberUtil.MatchType;
using PhoneNumberFormat = com.google.i18n.phonenumbers.PhoneNumberUtil.PhoneNumberFormat;
using PhoneMetadataCollection = com.google.i18n.phonenumbers.Phonemetadata.PhoneMetadataCollection;
using PhoneNumberDesc = com.google.i18n.phonenumbers.Phonemetadata.PhoneNumberDesc;
using Leniency = com.google.i18n.phonenumbers.PhoneNumberUtil.Leniency;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using com.google.i18n.phonenumbers;
using Test;
using java.lang;
using java.util.logging;
using java.util;




/**
 * Unittests for LRU Cache for compiled regular expressions used by the libphonenumbers libary.
 *
 * @author Shaopeng Jia
 */

[TestClass] public class RegexCacheTest : TestCase {
  private RegexCache regexCache;

  public RegexCacheTest() {
    regexCache = new RegexCache(2);
  }

  [TestMethod] public void testRegexInsertion() {
    String regex1  = "[1-5]";
    String regex2  = "(?:12|34)";
    String regex3  = "[1-3][58]";

    regexCache.getPatternForRegex(regex1);
    assertTrue(regexCache.containsRegex(regex1));

    regexCache.getPatternForRegex(regex2);
    assertTrue(regexCache.containsRegex(regex2));
    assertTrue(regexCache.containsRegex(regex1));

    regexCache.getPatternForRegex(regex1);
    assertTrue(regexCache.containsRegex(regex1));

    regexCache.getPatternForRegex(regex3);
    assertTrue(regexCache.containsRegex(regex3));

    assertFalse(regexCache.containsRegex(regex2));
    assertTrue(regexCache.containsRegex(regex1));
  }
}

}