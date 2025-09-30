using Matches.Generated;
using Matches.Generated.Module3;
using Matches.Generated.MyNamespace1;
using Module1.SubModule1;
using Module1.SubModule2;
using Module2.SomeModule;
using Module3;

namespace Matches.Tests
{
    [TestClass]
    public class GeneratedCodeTests
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
        public void SuccessWebRequestResultTest()
        {
            var requestData = new RequestData<string, Dictionary<string, int>, int>(new Dictionary<string, int> { { "ShowersDuringYear", 2 }, { "NutsPerDay", 228 } });

            var webRequestResult = 
                WebRequestResult.GetSuccessWebRequestResult<Dictionary<string, int>, int, WarningInfo>(requestData);

            var res = GetSummary(webRequestResult);

            Assert.AreEqual(string.Join(" ; ", requestData.Data.Select(x => $"{x.Key}:{x.Value}")), res);
        }

        [TestMethod]
        public void WarningsWebRequestResultTest()
        {
            List<WarningInfo> warningInfoList = [ new(1337, "you good man"), new(228, "run")];

            var webRequestResult =
                WebRequestResult.GetWarningsWebRequestResult<Dictionary<string, int>, int, WarningInfo>(warningInfoList);

            var res = GetSummary(webRequestResult);

            Assert.AreEqual(string.Join(" | ", warningInfoList.Select(x => $"Code {x.Code}: {x.Message}")), res);
        }

        [TestMethod]
        public void ErrorWebRequestResultTest()
        {
            List<string> errorList = ["help", "me", "please"];

            var webRequestResult =
                WebRequestResult.GetErrorsWebRequestResult<Dictionary<string, int>, int, WarningInfo>(errorList);

            var res = GetSummary(webRequestResult);

            Assert.AreEqual(string.Join(" ! ", errorList), res);
        }

        [TestMethod]
        public void EmailContactMatchVTest()
        {
            var email = new Email("aaa@bbb.com");
            var contact = Contact.GetEmailContact(email);
            var res = string.Empty;
            contact.MatchV(_ => res = "email", _ => res = "phone", _ => res = "webhook");
            Assert.AreEqual("email", res);
        }

        private static string SendMessage(IContact contact, string message) =>
            contact.Match
            (
                email => EmailHelper.SendMessage(email, message),
                phone => PhoneHelper.SendMessage(phone, message),
                webHook => WebHookHelper.SendMessage(webHook, message)
            );

        private static string GetSummary(IWebRequestResult<Dictionary<string, int>, int, WarningInfo> webRequestResult) => 
            webRequestResult.Match
            (
                requestData => string.Join(" ; ", requestData.Data.Select(x => $"{x.Key}:{x.Value}")),
                warningInfoList => string.Join(" | ", warningInfoList.Select(x => $"Code {x.Code}: {x.Message}")),
                errorList => string.Join(" ! ", errorList)
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
    public record Email(string Address);

    public static class EmailHelper
    {
        public static string SendMessage(Email email, string message) =>
            $"'{message}' sent to {email.Address} email address";
    }
}

namespace Module1.SubModule2
{
    public record Phone(int Code, int Number);

    public static class PhoneHelper
    {
        public static string SendMessage(Phone phone, string message) =>
            $"'{message}' sent to +{phone.Code}{phone.Number} phone number";
    }
}

namespace Module2.SomeModule
{
    public record WebHook(Uri Uri);

    public static class WebHookHelper
    {
        public static string SendMessage(WebHook webHook, string message) =>
            $"'{message}' sent to {webHook.Uri} webhook uri";
    }
}

namespace Module3
{
    [DiscriminatedUnion]
    public enum WebRequestResultKind
    {
        [DiscriminatedUnionCase(typeof(RequestData<,,>), typeof(string), null, typeof(GenericPlaceholder))]
        Success,
        [DiscriminatedUnionCase(typeof(List<>), (Type?)null)]
        Warnings,
        [DiscriminatedUnionCase(typeof(List<string>))]
        Errors
    }

    public record RequestData<TKey, TData, TItem>(TData Data) where TData : IDictionary<TKey, TItem> where TItem : struct;

    public record WarningInfo(int Code, string Message);
}

[DiscriminatedUnion]
public enum AnimalKind
{
    [DiscriminatedUnionCase(typeof(Dog))]
    Dog,

    [DiscriminatedUnionCase(typeof(Cat))]
    Cat,

    [DiscriminatedUnionCase(typeof(Alien<,>), typeof(GenericPlaceholder), null)]
    Alien
}

public class Dog;

public struct Cat;

public record Alien<TT, TK> where TT : TK;

[DiscriminatedUnion]
public enum OperationResultKind
{
    [DiscriminatedUnionCase(typeof(GenericPlaceholder))]
    Success,
    [DiscriminatedUnionCase(typeof(GenericPlaceholder), typeof(ClassConstraintPlaceholder), typeof(IEnumerable<string>), typeof(IList<GenericPlaceholder>))]
    Error,
    [DiscriminatedUnionCase(null)]
    Nothing
}
