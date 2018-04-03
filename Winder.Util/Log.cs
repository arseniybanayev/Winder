using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Winder.Util
{
	public interface ILogger
	{
		void Log(string message, Exception e = null);
		void Flush();
	}

	public abstract class LoggerBase : ILogger
	{
		private Queue<Action> _queue = new Queue<Action>();
		private AutoResetEvent _hasNewItems = new AutoResetEvent(false);
		private volatile bool _waiting = false;

		protected LoggerBase() {
			Thread loggingThread = new Thread(new ThreadStart(ProcessQueue));
			loggingThread.IsBackground = true;
			loggingThread.Start();
		}

		private void ProcessQueue() {
			while (true) {
				_waiting = true;
				AllDone();
				_hasNewItems.WaitOne(10000, true);
				_waiting = false;

				Queue<Action> queueCopy;
				lock (_queue) {
					queueCopy = new Queue<Action>(_queue);
					_queue.Clear();
				}

				foreach (var log in queueCopy) {
					log();
				}
			}
		}

		public void Log(string message, Exception e = null) {
			lock (_queue) {
				_queue.Enqueue(() => Write(FormatMessage(message, e)));
			}

			_hasNewItems.Set();
		}

		private const string Delimiter = ", ";

		public static string FormatMessage(string message, Exception e = null) {
			var s = new StringBuilder();

			// Add some diagnostic information (time and thread name)
			s.Append(DateTime.Now.ToString("HH:mm:ss.fff") + Delimiter);
			s.Append((Thread.CurrentThread.Name ?? "main") + ":" + Thread.CurrentThread.ManagedThreadId + Delimiter);

			// Add tagline message
			s.Append(message);

			if (e == null)
				return s.ToString();

			// Add formatted exception
			s.Append(Environment.NewLine + e.GetType().FullName);
			s.Append(Environment.NewLine + e.Message);
			s.Append(Environment.NewLine + e.StackTrace);
			var eInner = e.InnerException;
			var i = 0;
			while (eInner != null) {
				s.Append(Environment.NewLine + "Inner Exception " + (++i) + ": " + eInner.GetType().FullName + ": " + eInner.Message);
				s.Append(Environment.NewLine + eInner.StackTrace);
				eInner = eInner.InnerException;
			}

			return s.ToString();
		}

		public virtual void AllDone() { }
		public abstract void Write(string message);

		public void Flush() {
			while (!_waiting) {
				Thread.Sleep(1);
			}
		}
	}

	public class FileLogger : LoggerBase
	{
		public override void Write(string message) {
			GetWriter(DateTime.Today).WriteLine(message);
		}

		public override void AllDone() {
			base.AllDone();
			GetWriter(DateTime.Today).FlushAsync();
		}

		private TextWriter GetWriter(DateTime date) => TextWritersByDate.GetOrAdd(date, CreateSynchronizedTextWriter);

		private readonly LazyConcurrentDictionary<DateTime, TextWriter> TextWritersByDate
			= new LazyConcurrentDictionary<DateTime, TextWriter>();

		private static TextWriter CreateSynchronizedTextWriter(DateTime date) {
			var fileDirectory = Path.Combine(
				Path.GetPathRoot(Assembly.GetExecutingAssembly().Location),
				"logs",
				date.ToString("yyyy-MM-dd"));
			if (!Directory.Exists(fileDirectory))
				Directory.CreateDirectory(fileDirectory);
			var fileName = $"{Assembly.GetEntryAssembly().GetName().Name}-{Process.GetCurrentProcess().Id}.log";
			var stream = File.Open(
				Path.Combine(fileDirectory, fileName),
				FileMode.Append, FileAccess.Write, FileShare.ReadWrite);
			return TextWriter.Synchronized(new StreamWriter(stream));
		}
	}

	public class ConsoleLogger : LoggerBase
	{
		public override void Write(string message) {
			Console.WriteLine(message);
		}
	}

	public static class Log
	{
		private static readonly List<ILogger> Loggers = new List<ILogger>();

		public static void Add(ILogger logger) {
			Loggers.Add(logger);
		}

		public static void Info(string message) {
			WriteToAllLoggers(message);
		}

		public static void Error(string message, Exception e = null) {
			WriteToAllLoggers(message, e);
		}

		private static void WriteToAllLoggers(string message, Exception e = null) {
			foreach (var logger in Loggers)
				logger.Log(message, e);
		}

		public static void Flush() {
			Task.WaitAll(Loggers.Select(logger => Task.Run(() => logger.Flush())).ToArray());
		}
	}
}