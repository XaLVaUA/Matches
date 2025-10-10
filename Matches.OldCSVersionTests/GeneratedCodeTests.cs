using System;
using System.Threading.Tasks;
using Matches.Generated;
using Matches.Generated.MyNamespace1;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module1.SubModule1;
using Module1.SubModule2;
using Module2.SomeModule;

namespace Matches.OldCSVersionTests
{
    [TestClass]
    public sealed class GeneratedCodeTests
    {
        [TestMethod]
        public void EmailContactTest()
        {
            const string message = "hmm";
            var email = new Email("aaa@bbb.com");
            var contact = Contact.GetEmailContact(email);
            var res = SendMessage(contact, message);
            Assert.AreEqual($"'{message}' sent to {email.Address} email address", res);
        }

        [TestMethod]
        public void PhoneContactTest()
        {
            const string message = "hmm";
            var phone = new Phone(380, 1234567);
            var contact = Contact.GetPhoneContact(phone);
            var res = SendMessage(contact, message);
            Assert.AreEqual($"'{message}' sent to +{phone.Code}{phone.Number} phone number", res);
        }

        [TestMethod]
        public void WebHookContactTest()
        {
            const string message = "hmm";
            var webHook = new WebHook(new Uri("https://okak.kot"));
            var contact = Contact.GetWebHookContact(webHook);
            var res = SendMessage(contact, message);
            Assert.AreEqual($"'{message}' sent to {webHook.Uri} webhook uri", res);
        }

        [TestMethod]
        public async Task EmailContactTestAsync()
        {
            const string message = "hmm";
            var email = new Email("aaa@bbb.com");
            var contact = Contact.GetEmailContact(email);
            var res = await SendMessageAsync(contact, message);
            Assert.AreEqual($"'{message}' sent to {email.Address} email address", res);
        }

        [TestMethod]
        public async Task PhoneContactTestAsync()
        {
            const string message = "hmm";
            var phone = new Phone(380, 1234567);
            var contact = Contact.GetPhoneContact(phone);
            var res = await SendMessageAsync(contact, message);
            Assert.AreEqual($"'{message}' sent to +{phone.Code}{phone.Number} phone number", res);
        }

        [TestMethod]
        public async Task WebHookContactTestAsync()
        {
            const string message = "hmm";
            var webHook = new WebHook(new Uri("https://okak.kot"));
            var contact = Contact.GetWebHookContact(webHook);
            var res = await SendMessageAsync(contact, message);
            Assert.AreEqual($"'{message}' sent to {webHook.Uri} webhook uri", res);
        }

        private static string SendMessage(IContact contact, string message) =>
            contact.Match
            (
                email => EmailHelper.SendMessage(email, message),
                phone => PhoneHelper.SendMessage(phone, message),
                webHook => WebHookHelper.SendMessage(webHook, message)
            );

        private static Task<string> SendMessageAsync(IContact contact, string message) =>
            contact.MatchAsync
            (
                email => Task.FromResult(EmailHelper.SendMessage(email, message)),
                phone => Task.FromResult(PhoneHelper.SendMessage(phone, message)),
                webHook => Task.FromResult(WebHookHelper.SendMessage(webHook, message))
            );
    }
}

namespace MyNamespace1
{
    [DiscriminatedUnion]
    public enum ContactKind
    {
        [DiscriminatedUnionCase(typeof(Email))]
        Email,
        [DiscriminatedUnionCase(typeof(Phone))]
        Phone,
        [DiscriminatedUnionCase(typeof(WebHook))]
        WebHook
    }
}

namespace Module1.SubModule1
{
    public class Email
    {
        public string Address { get; }

        public Email(string address)
        {
            Address = address;
        }
    }

    public static class EmailHelper
    {
        public static string SendMessage(Email email, string message) =>
            $"'{message}' sent to {email.Address} email address";
    }
}

namespace Module1.SubModule2
{
    public class Phone
    {
        public int Code { get; }

        public int Number { get; }

        public Phone(int code, int number)
        {
            Code = code;
            Number = number;
        }
    }

    public static class PhoneHelper
    {
        public static string SendMessage(Phone phone, string message) =>
            $"'{message}' sent to +{phone.Code}{phone.Number} phone number";
    }
}

namespace Module2.SomeModule
{
    public class WebHook
    {
        public Uri Uri { get; }

        public WebHook(Uri uri)
        {
            Uri = uri;
        }
    }

    public static class WebHookHelper
    {
        public static string SendMessage(WebHook webHook, string message) =>
            $"'{message}' sent to {webHook.Uri} webhook uri";
    }
}
