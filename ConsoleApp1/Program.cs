
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ConsoleApp1
{
	class Program
	{
		static void Main(string[] args)
		{

			var mailRepository = new MailRepository("imap.gmail.com", 993, true, "email", "password");
			var allEmails = mailRepository.GetAllMails();
			var count = 0;
			foreach (var email in allEmails)
			{
				if (
					email.From.ToString().ToLower().Contains("@indeed.com") ||
				   email.From.ToString().ToLower().Contains("careerbuilder")
					)
					continue;
				if (
					email.Subject.ToLower().Contains(".net") ||
					email.Subject.ToLower().Contains(".remote") ||
					email.Subject.ToLower().Contains(".angular") ||
					email.Subject.ToLower().Contains(".hiring") ||
					email.Subject.ToLower().Contains(".urgent needs") ||
					email.Subject.ToLower().Contains(".net developer") ||
					email.Subject.ToLower().Contains("software Engineer") ||
					email.Subject.ToLower().Contains("job opening") ||
					email.Subject.ToLower().Contains("urgent opening") ||
					email.Subject.ToLower().Contains("opening") ||
					email.Subject.ToLower().Contains("contract") ||
					email.Subject.ToLower().Contains("dot net") ||
					email.Subject.ToLower().Contains("backend") ||
					email.Subject.ToLower().Contains("developer")
					)
					if (email.HtmlBody != null)
					{
						if (
							email.HtmlBody.ToLower().Contains("asp.net") ||
							email.HtmlBody.ToLower().Contains("c#") ||
							email.HtmlBody.ToLower().Contains(".net") ||
							email.HtmlBody.ToLower().Contains(".angular")
							)

							count++;
						mailRepository.Reply(email, false);
					}
					else if (email.TextBody != null)
					{
						if (
						   email.TextBody.ToLower().Contains("asp.net") ||
						   email.TextBody.ToLower().Contains("c#") ||
						   email.TextBody.ToLower().Contains(".net") ||
						   email.TextBody.ToLower().Contains(".angular")
						   )

							mailRepository.Reply(email, true);
						count++;
					}
				Console.WriteLine("sent " + count + "/" + allEmails.Count());
				Console.WriteLine("=======================================================");
			}
			

		}

	}
	public class MailRepository
	{
		private readonly string mailServer, login, password;
		private readonly int port;
		private readonly bool ssl;

		public MailRepository(string mailServer, int port, bool ssl, string login, string password)
		{
			this.mailServer = mailServer;
			this.port = port;
			this.ssl = ssl;
			this.login = login;
			this.password = password;
		}

		public IEnumerable<string> GetUnreadMails()
		{
			var messages = new List<string>();

			using (var client = new ImapClient())
			{
				client.Connect(mailServer, port, ssl);

				// Note: since we don't have an OAuth2 token, disable
				// the XOAUTH2 authentication mechanism.
				client.AuthenticationMechanisms.Remove("XOAUTH2");

				client.Authenticate(login, password);

				// The Inbox folder is always available on all IMAP servers...
				var inbox = client.Inbox;
				inbox.Open(FolderAccess.ReadOnly);
				var results = inbox.Search(SearchOptions.All, SearchQuery.Not(SearchQuery.Seen));
				foreach (var uniqueId in results.UniqueIds)
				{
					var message = inbox.GetMessage(uniqueId);

					messages.Add(message.HtmlBody);

					//Mark message as read
					//inbox.AddFlags(uniqueId, MessageFlags.Seen, true);
				}

				client.Disconnect(true);
			}

			return messages;
		}

		public IEnumerable<MimeMessage> GetAllMails()
		{
			var messages = new List<MimeMessage>();


			using (var client = new ImapClient())
			{
				client.Connect(mailServer, port, ssl);

				// Note: since we don't have an OAuth2 token, disable
				// the XOAUTH2 authentication mechanism.
				client.AuthenticationMechanisms.Remove("XOAUTH2");

				client.Authenticate(login, password);

				// The Inbox folder is always available on all IMAP servers...
				var inbox = client.Inbox;
				inbox.Open(FolderAccess.ReadOnly);
				var results = inbox.Search(SearchOptions.All, SearchQuery.NotSeen);
				foreach (var uniqueId in results.UniqueIds)
				{
					var message = inbox.GetMessage(uniqueId);

					messages.Add(message);

					//Mark message as read
					//inbox.AddFlags(uniqueId, MessageFlags.Seen, true);
				}

				client.Disconnect(true);
			}

			return messages;
		}

		public MimeMessage Reply(MimeMessage message, bool replyToAll)
		{
			var reply = new MimeMessage();

			// reply to the sender of the message
			if (message.ReplyTo.Count > 0)
			{
				reply.To.AddRange(message.ReplyTo);
			}
			else if (message.From.Count > 0)
			{
				reply.To.AddRange(message.From);
			}
			else if (message.Sender != null)
			{
				reply.To.Add(message.Sender);
			}

			if (replyToAll)
			{
				// include all of the other original recipients - TODO: remove ourselves from these lists
				reply.To.AddRange(message.To);
				reply.Cc.AddRange(message.Cc);
			}

			// set the reply subject
			if (!message.Subject.StartsWith("Re:", StringComparison.OrdinalIgnoreCase))
				reply.Subject = "Re:" + message.Subject;
			else
				reply.Subject = message.Subject;

			// construct the In-Reply-To and References headers
			if (!string.IsNullOrEmpty(message.MessageId))
			{
				reply.InReplyTo = message.MessageId;
				foreach (var id in message.References)
					reply.References.Add(id);
				reply.References.Add(message.MessageId);
			}

			// quote the original message text
			using (var quoted = new StringWriter())
			{
				var sender = message.Sender ?? message.From.Mailboxes.FirstOrDefault();

				//quoted.WriteLine("On {0}, {1} wrote:", message.Date.ToString("f"), !string.IsNullOrEmpty(sender.Name) ? sender.Name : sender.Address);
				//using (var reader = new StringReader(message.TextBody))
				//{
				//    string line;

				//    while ((line = reader.ReadLine()) != null)
				//    {
				//        quoted.Write("> ");
				//        quoted.WriteLine(line);
				//    }
				//}
				reply.From.AddRange(message.From);
				var bodyBuilder = new BodyBuilder();
				var stream = System.IO.File.ReadAllBytes(@"../../../Moiez_CV.pdf");
					
				bodyBuilder.Attachments.Add(@"Moiez_CV.pdf",stream);
				bodyBuilder.HtmlBody = File.ReadAllText(@"../../../MessageBody.txt");
				reply.Body = bodyBuilder.ToMessageBody();

			}

			using (var client = new MailKit.Net.Smtp.SmtpClient())
			{
				client.Connect("smtp.gmail.com", 587, SecureSocketOptions.StartTls);

				// Note: since we don't have an OAuth2 token, disable
				// the XOAUTH2 authentication mechanism.
				client.AuthenticationMechanisms.Remove("XOAUTH2");

				client.Authenticate(login, password);
				client.Send(reply);
				Console.WriteLine("Role ==> " + reply.Subject);
				Console.WriteLine("Sending Application to ==> " + reply.To.FirstOrDefault());


				client.Disconnect(true);
			}
			return reply;
		}
	}
}
