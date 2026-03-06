using System;
using NUnit.Framework;
using TinyConfig.Internal;

namespace TinyConfig.Tests
{
    public enum SampleEnum { None, Alpha, Beta }

    [TestFixture]
    public class ValueConverterTests
    {
        [Test]
        public void Convert_int()
        {
            Assert.That(ValueConverter.Convert("123", 0), Is.EqualTo(123));
        }

        [Test]
        public void Convert_bool_true()
        {
            Assert.That(ValueConverter.Convert("True", false), Is.True);
        }

        [Test]
        public void Convert_bool_false()
        {
            Assert.That(ValueConverter.Convert("False", true), Is.False);
        }

        [Test]
        public void Convert_double()
        {
            Assert.That(ValueConverter.Convert("3.14", 0.0), Is.EqualTo(3.14).Within(0.001));
        }

        [Test]
        public void Convert_enum()
        {
            Assert.That(ValueConverter.Convert("Beta", SampleEnum.None), Is.EqualTo(SampleEnum.Beta));
        }

        [Test]
        public void Convert_null_returns_default()
        {
            Assert.That(ValueConverter.Convert(null, 42), Is.EqualTo(42));
        }

        [Test]
        public void Convert_empty_returns_default()
        {
            Assert.That(ValueConverter.Convert("", 99), Is.EqualTo(99));
        }

        [Test]
        public void Convert_whitespace_returns_default()
        {
            Assert.That(ValueConverter.Convert("   ", 7), Is.EqualTo(7));
        }

        [Test]
        public void Convert_invalid_format_returns_default()
        {
            Assert.That(ValueConverter.Convert("not_a_number", 0), Is.EqualTo(0));
        }

        [Test]
        public void Convert_invalid_enum_returns_default()
        {
            Assert.That(ValueConverter.Convert("InvalidValue", SampleEnum.None), Is.EqualTo(SampleEnum.None));
        }
    }
}
