// Author: Justin Ngo
// Created: Feburary 2022
// About: This is a console app that returns zero when emails are sent via SMTP server succeeds.

// TODO: 02/21/2022 - Add configuration file for port and server instead of just command line
// TODO: 02/21/2022 - Enable MIME (Multipurpose Internet Mail Extnesion) so we can send other things besides just plain text.
// TODO: 02/23/2022 - Aliases for options separated by a pipe so you can use either "-p|--port" so you'll need to do something so that it matches the key since it will be only partial
//						an idea can be just have the key be numbers and then you'll have other things so that you you do a partial string match and if it matches in another enum then 
//						you can assign it and match on that enum instead. 

using System;
using System.Collections.Generic;
using MimeKit; // Documentation: http://www.mimekit.net/docs/html/Introduction.htm
using MailKit.Net.Smtp; // Installed via NuGet: Install-Package MailKit
using MailKit;
// using System.Net.Mail; // Blocking method that uses SMTP server for email.
// 02/21/2022 - Does not recommend using this because doesn't support modern protocols. 
// https://docs.microsoft.com/en-us/dotnet/api/system.net.mail.smtpclient?view=net-6.0

// try NDsk.options a more fleshed out option parser for C#

namespace ConsoleSendMail
{
	class Program
	{
		static int Main(string[] args)
		{
			int port = 0;
			string? host = null;    // i.e. "smtp.gmail.com"
			string? body = null;    // i.e. "Some Text"
			string? subject = null;	// i.e. "Some Text"
			string? cc = null;		// i.e. same as "to" and "from"
			string? bcc = null;		// i.e. same as "to" and "from"
			string? to = null;		// i.e. "recipient@something.com; recipient@something2.com ..."
			string? from = null;	// i.e. "personal@something.com; personal@something2.com ..."
			string? attach = null;	// i.e. "C:\Users\Kronstadt\Documents\party.ics"
			string? username = null;// i.e. client.authenticate(username, password)
			string? password = null;
			int SslOptions = 0;		// 0 No TLS/SSL, 1 = Auto, 2 = SSL/TLS immediately, 3 = TLS, 4 = TLS when available
			bool useAsync = false;
			bool help = false;
			bool version = false;

			// using Parameters = System.Collections.Generic.Dictionary<string,System.Action<string>>;
			//		Basically doing this you can replace everywhere with Dictionary<string, Action<string>> with just "Parameters"
			// Action (no need to return anything) and Func (at least 1 return generally furthest right parameter) are delegates (pointer to methods)
			//		not to be confused with lambdas (anonymous functions)
			// Parameter List
			var parameters = new Dictionary<string, Action<string>>{
				{"--port", x => port = CliProcessParams<int>(x)}, // 25 = clear text, 465 = ssl, 587 = TLS
				{"--host", x => host = x.Trim()},
				{"--body", x => body = x.Trim()},
				{"--subject", x => subject = x.Trim()},
				{"--cc", x => cc = x.Trim()},
				{"--bcc", x => bcc = x.Trim()},
				{"--to", x => to = x.Trim()},
				{"--from", x => from = x.Trim()},
				{"--attach", x => attach = x.Trim()},
				{"--username", x => username = x.Trim()},
				{"--password", x => password = x.Trim()},
				{"--sslOptions", x => SslOptions = CliProcessParams<int>(x) },
				{"--useAsync", _ => useAsync = true },
				{"--help", _ => help = true },
				{"--version", _ => version = true }
			};
			if (args.Length > 0)
			{
				CliParse(parameters, args);
			}
			else
			{
				showHelp();
				return 0;
			}

			if (help)
			{
				showHelp();
				return 0;
			}

			if (version)
			{
				showVersion();
				return 0;
			}

			// create MIME message
			var message = new MimeMessage();
			char[] delimiters = new char[] { ',', ';' };
			if (to != null)
			{
				// accepts comma-separated list so... new MailMessage(fromMail, addresses.replace(";", ",")
				foreach (var address in to.Split(delimiters, StringSplitOptions.RemoveEmptyEntries))
				{
					message.To.Add(new MailboxAddress(null, address.Trim()));
				}
			}
			if (from != null)
			{
				foreach (var address in from.Split(delimiters, StringSplitOptions.RemoveEmptyEntries))
				{
					message.From.Add(new MailboxAddress(null, address.Trim()));
				}
			}
			if (cc != null)
			{
				foreach (var address in cc.Split(delimiters, StringSplitOptions.RemoveEmptyEntries))
				{
					message.Cc.Add(new MailboxAddress(null, address.Trim()));
				}
			}
			if (bcc != null)
			{
				foreach (var address in bcc.Split(delimiters, StringSplitOptions.RemoveEmptyEntries))
				{
					message.Bcc.Add(new MailboxAddress(null, address.Trim()));
				}
			}
			message.Subject = subject;

			// create body
			var builder = new BodyBuilder();
			builder.TextBody = body;
			if (attach != null) builder.Attachments.Add(attach);

			message.Body = builder.ToMessageBody();

			if (!useAsync)
			{
				using (var client = new SmtpClient())
				{
					client.Connect(host, port, (MailKit.Security.SecureSocketOptions)SslOptions); // Casted Integer to Enum
					// alternative way of casting: (MailKit.Security.SecureSocketOptions)Enum.ToObject(typeof(MailKit.Security.SecureSocketOptions), SslOptions)
					if (username != null && password != null)
					{
						client.Authenticate(username, password);
					}
					client.Send(message);
					client.Disconnect(true);
				}
			}
			else
			{
				throw new Exception("Async not implemented yet");
			}

			Console.WriteLine("Email sent succesfully");
			return 0; // Emails Sent 
		}

		public static void CliParse(Dictionary<string, Action<string>> parameters, string[] args)
		{
			Action<string>? currentCallback = null;
			foreach (var arg in args)
			{
				// what happens where is that the dictionary finds the key and then puts "out" the Action<string> into CliProcess
				if (parameters.TryGetValue(arg, out var CliProcess))
				{
					// if it is a boolean parameter
					currentCallback?.Invoke("");
					currentCallback = CliProcess;
				}
				else if (currentCallback != null)
				{
					currentCallback(arg);
					currentCallback = null;
				}
				else
				{
					showHelp();
					throw new Exception("Unknown parameter: " + arg);
				}
			}

			if (currentCallback != null)
			{
				// if a bool paramter is at the end
				currentCallback("");
			}
		}

		// Generic, Honestly this maybe overkill but it is good practice for me
		public static T? CliProcessParams<T>(string x) where T : IConvertible
		{
			// Iconvertible is limiting the scope of what T can be (reference and value types that are normally seen like string, int, bools etc. 
			if (typeof(T) == typeof(int))
			{
				try
				{
					int result = Int32.Parse(x.Trim());
					return (T) Convert.ChangeType(result, typeof(T));
				}
				catch (FormatException)
				{
					Console.WriteLine($"Unable to parse: '{x}'");
				}
			}
			// Don't need this cause we are just seeing if it something is present.
			//else if (typeof(T) == typeof(bool))
			//{
			//	if (x.Equals("T", StringComparison.OrdinalIgnoreCase) || x.Equals("true", StringComparison.OrdinalIgnoreCase))
			//	{
			//		return (T) Convert.ChangeType(true, typeof(T));
			//	}
			//}

			//https://docs.microsoft.com/en-us/dotnet/csharp/language-reference/keywords/default-values-table
			// T (reference type) => returns null or nullable 
			// T (int) => 0
			// T ('\0') => char
			// where T : Class to constrain then use can use null as normal
			return default(T);
		}

		public static string showVersion()
		{
			string version = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
			if (version != null)
			{
				return version;
			}
			else
			{
				return "";
			}
		}

		public static void showHelp()
		{
			Console.WriteLine("ConsoleSendMail");
			Console.WriteLine("	Simple console application to programmatically send email using a smtp server");
			Console.WriteLine();
			Console.WriteLine("Usage:");
			Console.WriteLine("	command [options] [arguments]");
			Console.WriteLine("		example: ConsoleSendMail.exe --to \"sample@sample.com, sample2@sample.com\"");
			Console.WriteLine("									 --from \"sample@sample.com\"");
			Console.WriteLine("									 --subject \"Example console send mail call\"");
			Console.WriteLine("									 --body \"This is an example\"");
			Console.WriteLine("									 --port 25");
			Console.WriteLine("									 --host smtp.gmail.com");
			Console.WriteLine("Options:");
			Console.WriteLine("	--help			Display help message");
			Console.WriteLine("	--version		App version");
			Console.WriteLine("	--port			Port Number");
			Console.WriteLine("	--host			Host to connect to");
			Console.WriteLine("	--to");
			Console.WriteLine("	--from");
			Console.WriteLine("	--cc");
			Console.WriteLine("	--bcc");
			Console.WriteLine("	--subject");
			Console.WriteLine("	--body");
			Console.WriteLine("	--attach		Path to attachment");
			Console.WriteLine("	--username");
			Console.WriteLine("	--password");
			Console.WriteLine("	--sslOptions	0(default)=none, 1=auto, 2=SSL/TLS at start");
			Console.WriteLine("	--useAsync		TODO: send mail asynchronously");
		}
	}

	// This is not used... your smtp server needs to have DNS enabled or it won't work. 
	// var supportsDsn = client.Capabilities.HasFlag(SmtpCapabilities.Dsn);
	//class DeliveryStatusNotificationSmtp : SmtpClient
	//{
	//	protected override DeliveryStatusNotification? GetDeliveryStatusNotifications(MimeMessage message, MailboxAddress mailbox)
	//	{
	//			return DeliveryStatusNotification.Success;
	//	}
	//}
}