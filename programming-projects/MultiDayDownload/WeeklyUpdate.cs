#region Using declarations
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Gui.Tools;
using NinjaTrader.NinjaScript;
#endregion

// WeeklyUpdate - Downloads last 14 days of replay data for ES, MES, NQ, MNQ, YM, MYM, RTY, M2K, GC, MGC
// 2026-0417 GreyBeard - Initial version based on MultidayReplayDownloaderWindowEN

namespace NinjaTrader.Gui.NinjaScript
{
	public class WeeklyUpdate : AddOnBase
	{
		private NTMenuItem menuItem;
		private NTMenuItem existingMenuItemInControlCenter;
		private WeeklyUpdateWindow weeklyUpdateWindow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "WeeklyUpdate";
				Description = "Download last 14 days of replay data for ES, MES, NQ, MNQ, YM, MYM, RTY, M2K, GC, MGC";
			}
			else if (State == State.Terminated)
			{
				if (weeklyUpdateWindow != null)
				{
					weeklyUpdateWindow.Dispatcher.InvokeAsync(() =>
					{
						weeklyUpdateWindow.Close();
						weeklyUpdateWindow = null;
					});
				}
			}
		}

		protected override void OnWindowCreated(Window window)
		{
			ControlCenter cc = window as ControlCenter;
			if (cc == null) return;

			existingMenuItemInControlCenter = cc.FindFirst("ControlCenterMenuItemTools") as NTMenuItem;
			if (existingMenuItemInControlCenter == null) return;

			menuItem = new NTMenuItem
			{
				Header = "Weekly Update",
				Style = Application.Current.TryFindResource("MainMenuItem") as Style
			};
			existingMenuItemInControlCenter.Items.Add(menuItem);
			menuItem.Click += OnMenuItemClick;
		}

		protected override void OnWindowDestroyed(Window window)
		{
			if (menuItem != null && window is ControlCenter)
			{
				if (existingMenuItemInControlCenter != null && existingMenuItemInControlCenter.Items.Contains(menuItem))
					existingMenuItemInControlCenter.Items.Remove(menuItem);
				menuItem.Click -= OnMenuItemClick;
				menuItem = null;
			}
		}

		private void OnMenuItemClick(object sender, RoutedEventArgs e)
		{
			Core.Globals.RandomDispatcher.BeginInvoke(new Action(() =>
			{
				if (weeklyUpdateWindow == null)
				{
					weeklyUpdateWindow = new WeeklyUpdateWindow();
					weeklyUpdateWindow.Closed += (s, args) => weeklyUpdateWindow = null;
					weeklyUpdateWindow.Show();
				}
				else
				{
					weeklyUpdateWindow.Activate();
				}
			}));
		}
	}

	public class WeeklyUpdateWindow : NTWindow, IWorkspacePersistence
	{
		// UI Controls
		private Button btnDownload;
		private Button btnClearLog;
		private CheckBox chkSkipExisting;
		private TextBox tbOutput;
		private Label lProgress;
		private ProgressBar pbProgress;

		// State
		private bool isRunning = false;
		private bool isCanceling = false;
		private int completedDays = 0;
		private int totalDays = 0;
		private DateTime startTimestamp;
		private List<WUDownloadEntry> downloadQueue;
		private int currentDownloadIndex = 0;
		private int totalInstruments = 0;

		// Contract months per instrument root
		private static readonly string[] QuarterlyMonths = { "03", "06", "09", "12" };
		private static readonly string[] GoldMonths     = { "02", "04", "06", "08", "10", "12" };

		private static readonly string[] TargetRoots = { "ES", "MES", "NQ", "MNQ", "YM", "MYM", "RTY", "M2K", "GC", "MGC" };

		public WeeklyUpdateWindow()
		{
			Caption = "Weekly Update v1.1";
			Width = 550;
			Height = 400;
			Content = BuildContent();

			Loaded += (o, e) =>
			{
				if (WorkspaceOptions == null)
					WorkspaceOptions = new WorkspaceOptions("WeeklyUpdate-" + Guid.NewGuid().ToString("N"), this);
			};

			Closing += OnWindowClosing;
		}

		// Returns "ROOT MM-YY" for the current active contract
		private string GetCurrentContract(string root)
		{
			int year = DateTime.Now.Year;
			int month = DateTime.Now.Month;

			// CL and MCL: expires ~20th of the prior month, so active contract is current month + 2
			if (root == "CL" || root == "MCL")
			{
				int contractMonth = month + 2;
				int contractYear  = year;
				if (contractMonth > 12) { contractMonth -= 12; contractYear++; }
				return string.Format("{0} {1:D2}-{2:D2}", root, contractMonth, contractYear % 100);
			}

			string[] months = (root == "GC" || root == "MGC") ? GoldMonths : QuarterlyMonths;

			foreach (var m in months)
			{
				if (int.Parse(m) >= month)
					return string.Format("{0} {1}-{2:D2}", root, m, year % 100);
			}

			// Wrap to next year
			return string.Format("{0} {1}-{2:D2}", root, months[0], (year + 1) % 100);
		}

		// Returns the contract(s) to download for a root. Normally just the current
		// contract, but during rollover week (expiration within 7 days either side
		// of today) both the old and new contracts are returned.
		private List<string> GetActiveContracts(string root)
		{
			var contracts = new List<string> { GetCurrentContract(root) };

			int month, year;
			ParseContract(contracts[0], out month, out year);

			DateTime expiration = GetExpirationDate(root, month, year);
			if (Math.Abs((expiration.Date - DateTime.Today).TotalDays) <= 7)
				contracts.Add(GetNextContract(root, month, year));

			return contracts;
		}

		// Parses "ROOT MM-YY" into month (1-12) and year (20YY)
		private static void ParseContract(string contract, out int month, out int year)
		{
			string[] parts = contract.Split(' ')[1].Split('-');
			month = int.Parse(parts[0]);
			year  = 2000 + int.Parse(parts[1]);
		}

		// Returns the approximate last-trading-day (expiration) date for a contract
		private static DateTime GetExpirationDate(string root, int month, int year)
		{
			// CL/MCL expire ~20th of the month prior to the contract month
			if (root == "CL" || root == "MCL")
			{
				int expMonth = month - 1;
				int expYear  = year;
				if (expMonth < 1) { expMonth = 12; expYear--; }
				return new DateTime(expYear, expMonth, 20);
			}

			// GC/MGC expire on the 3rd-to-last business day of the contract month
			if (root == "GC" || root == "MGC")
				return ThirdToLastBusinessDay(year, month);

			// ES/NQ/YM and minis expire on the 3rd Friday of the contract month
			return ThirdFriday(year, month);
		}

		private static DateTime ThirdFriday(int year, int month)
		{
			DateTime firstOfMonth = new DateTime(year, month, 1);
			int daysUntilFriday = ((int)DayOfWeek.Friday - (int)firstOfMonth.DayOfWeek + 7) % 7;
			return firstOfMonth.AddDays(daysUntilFriday + 14);
		}

		private static DateTime ThirdToLastBusinessDay(int year, int month)
		{
			DateTime date = new DateTime(year, month, DateTime.DaysInMonth(year, month));
			int businessDays = 0;
			while (true)
			{
				if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
				{
					businessDays++;
					if (businessDays == 3) return date;
				}
				date = date.AddDays(-1);
			}
		}

		// Returns the next contract in the rotation for the given root
		private static string GetNextContract(string root, int month, int year)
		{
			int step = (root == "CL" || root == "MCL") ? 1 : (root == "GC" || root == "MGC") ? 2 : 3;
			int nextMonth = month + step;
			int nextYear  = year;
			if (nextMonth > 12) { nextMonth -= 12; nextYear++; }
			return string.Format("{0} {1:D2}-{2:D2}", root, nextMonth, nextYear % 100);
		}

		private DependencyObject BuildContent()
		{
			const double margin   = 10;
			const double spacing  = 8;
			const double btnH     = 26;
			const double btnW     = 110;
			const double fontSize = 12;

			Grid mainGrid = new Grid { Background = new SolidColorBrush(Colors.Transparent) };
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 0: description
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 1: contract list
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 2: options
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 3: download button
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });   // Row 4: log
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 5: progress label
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // Row 6: progress bar

			Brush labelBrush = TryFindResource("FontLabelBrush") as Brush ?? Brushes.White;

			// Row 0: Description
			Label lblDesc = new Label
			{
				Content = "Downloads the last 14 days of market replay data.",
				Margin = new Thickness(margin, margin, margin, spacing / 2),
				Foreground = labelBrush,
				FontSize = fontSize
			};
			Grid.SetRow(lblDesc, 0);
			mainGrid.Children.Add(lblDesc);

			// Row 1: Current contract list
			string contracts = string.Join("   ", TargetRoots.SelectMany(r => GetActiveContracts(r)));
			Label lblContracts = new Label
			{
				Content = new TextBlock { Text = contracts, TextWrapping = TextWrapping.Wrap },
				Margin = new Thickness(margin, 0, margin, spacing),
				Foreground = labelBrush,
				FontSize = fontSize,
				FontWeight = FontWeights.Bold
			};
			Grid.SetRow(lblContracts, 1);
			mainGrid.Children.Add(lblContracts);

			// Row 2: Options
			StackPanel optionsPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(margin, 0, margin, spacing)
			};

			chkSkipExisting = new CheckBox
			{
				IsChecked = false,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 4, 0)
			};

			Label lblSkipExisting = new Label
			{
				Content = "Skip Existing Replay Files",
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = labelBrush,
				FontSize = fontSize,
				Padding = new Thickness(0),
				Margin = new Thickness(0)
			};
			// Make label non-clickable (only checkbox is clickable)
			lblSkipExisting.MouseDown += (s, e) => e.Handled = true;

			optionsPanel.Children.Add(chkSkipExisting);
			optionsPanel.Children.Add(lblSkipExisting);
			Grid.SetRow(optionsPanel, 2);
			mainGrid.Children.Add(optionsPanel);

			// Row 3: Download button
			btnDownload = new Button
			{
				Content = "_Download",
				IsDefault = true,
				Height = btnH,
				Width = btnW,
				Padding = new Thickness(8, 0, 8, 0),
				Margin = new Thickness(margin, 0, margin, spacing),
				FontSize = fontSize,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			btnDownload.Click += BtnDownload_Click;
			Grid.SetRow(btnDownload, 3);
			mainGrid.Children.Add(btnDownload);

			// Row 4: Log header + log output
			Grid logPanel = new Grid();
			logPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			logPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

			Grid logHeaderGrid = new Grid { Margin = new Thickness(margin, 0, margin, spacing / 2) };
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

			Label lblLog = new Label
			{
				Content = "Status Log：",
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, spacing, 0),
				Padding = new Thickness(0)
			};
			Grid.SetColumn(lblLog, 0);
			logHeaderGrid.Children.Add(lblLog);

			btnClearLog = new Button
			{
				Content = "Clear",
				Height = btnH,
				Width = 80,
				Padding = new Thickness(8, 0, 8, 0),
				FontSize = fontSize
			};
			btnClearLog.Click += BtnClearLog_Click;
			Grid.SetColumn(btnClearLog, 1);
			logHeaderGrid.Children.Add(btnClearLog);

			tbOutput = new TextBox
			{
				IsReadOnly = true,
				HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
				VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
				Margin = new Thickness(margin, 0, margin, margin),
				MinHeight = 120,
				TextWrapping = TextWrapping.Wrap,
				FontSize = fontSize,
				FontFamily = new FontFamily("Consolas")
			};

			Grid.SetRow(logHeaderGrid, 0);
			Grid.SetRow(tbOutput, 1);
			logPanel.Children.Add(logHeaderGrid);
			logPanel.Children.Add(tbOutput);
			Grid.SetRow(logPanel, 4);
			mainGrid.Children.Add(logPanel);

			// Row 5: Progress Label
			lProgress = new Label
			{
				Content = "",
				Height = 0,
				Margin = new Thickness(margin, 0, margin, spacing / 2),
				Foreground = labelBrush,
				FontSize = fontSize
			};
			Grid.SetRow(lProgress, 5);
			mainGrid.Children.Add(lProgress);

			// Row 6: Progress Bar
			pbProgress = new ProgressBar
			{
				Height = 0,
				Margin = new Thickness(margin, 0, margin, margin)
			};
			Grid.SetRow(pbProgress, 6);
			mainGrid.Children.Add(pbProgress);

			return mainGrid;
		}

		private void BtnClearLog_Click(object sender, RoutedEventArgs e)
		{
			if (tbOutput != null)
				tbOutput.Clear();
		}

		private void BtnDownload_Click(object sender, RoutedEventArgs e)
		{
			if (isRunning)
			{
				if (!isCanceling)
				{
					isCanceling = true;
					LogMessage("Canceling download...");
					btnDownload.IsEnabled = false;
					btnDownload.Content = "Canceling...";
				}
				return;
			}

			BuildDownloadQueue();

			if (downloadQueue.Count == 0)
			{
				LogMessage("No days to download — all files already exist for all instruments.");
				return;
			}

			StartDownloadProcess();
		}

		private void BuildDownloadQueue()
		{
			downloadQueue = new List<WUDownloadEntry>();

			DateTime endDate   = DateTime.Today.AddDays(-1);
			DateTime startDate = DateTime.Today.AddDays(-14);

			// Count Saturdays in range once (same dates for all instruments)
			int skippedSaturdays = 0;
			for (DateTime d = startDate; d <= endDate; d = d.AddDays(1))
				if (d.DayOfWeek == DayOfWeek.Saturday) skippedSaturdays++;

			int skippedExisting   = 0;
			int instrumentErrors  = 0;

			totalInstruments = 0;

			foreach (string root in TargetRoots)
			{
				List<string> activeContracts = GetActiveContracts(root);
				totalInstruments += activeContracts.Count;
				if (activeContracts.Count > 1)
					LogMessage(string.Format("Rollover week for {0}: downloading {1}.", root, string.Join(" and ", activeContracts)));

				foreach (string instrumentName in activeContracts)
				{
					Instrument instrument = null;

					try { instrument = Instrument.GetInstrument(instrumentName); }
					catch { }

					if (instrument == null)
					{
						LogMessage(string.Format("WARNING: '{0}' not found — skipping.", instrumentName));
						instrumentErrors++;
						continue;
					}

					string replayDir = Path.Combine(Core.Globals.UserDataDir, "db", "replay", instrument.FullName);

					for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
					{
						if (date.DayOfWeek == DayOfWeek.Saturday) continue;

						string nrdPath = Path.Combine(replayDir, date.ToString("yyyyMMdd") + ".nrd");

						if (chkSkipExisting.IsChecked == true && File.Exists(nrdPath))
						{
							skippedExisting++;
							continue;
						}

						downloadQueue.Add(new WUDownloadEntry
						{
							Date       = date,
							Instrument = instrument,
							NrdPath    = nrdPath,
							Status     = WUDownloadStatus.Pending
						});
					}
				}
			}

			LogMessage(string.Format("Range: {0:yyyy-MM-dd} to {1:yyyy-MM-dd}", startDate, endDate));
			if (skippedSaturdays > 0)  LogMessage(string.Format("Skipped {0} Saturday(s).", skippedSaturdays));
			if (skippedExisting > 0)   LogMessage(string.Format("Skipped {0} already-existing file(s).", skippedExisting));
			if (instrumentErrors > 0)  LogMessage(string.Format("WARNING: {0} instrument(s) could not be resolved.", instrumentErrors));

			totalDays          = downloadQueue.Count;
			completedDays      = 0;
			currentDownloadIndex = 0;
		}

		private void StartDownloadProcess()
		{
			isRunning  = true;
			isCanceling = false;
			startTimestamp = DateTime.Now;

			btnDownload.Content   = "_Cancel";
			btnDownload.IsEnabled = true;
			chkSkipExisting.IsEnabled = false;

			const double m = 8;
			pbProgress.Minimum = 0;
			pbProgress.Maximum = totalDays;
			pbProgress.Value   = 0;
			pbProgress.Height  = 16;
			pbProgress.Margin  = new Thickness(m, 0, m, m);
			lProgress.Height   = 24;

			LogMessage(string.Format("Queued {0} download(s) across {1} instrument(s).", totalDays, totalInstruments));
			Globals.RandomDispatcher.InvokeAsync(new Action(() => DownloadNextDay()));
		}

		private void DownloadNextDay()
		{
			if (isCanceling || currentDownloadIndex >= downloadQueue.Count)
			{
				CompleteDownloadProcess();
				return;
			}

			WUDownloadEntry entry = downloadQueue[currentDownloadIndex];
			entry.Status = WUDownloadStatus.InProgress;

			Dispatcher.InvokeAsync(() =>
				LogMessage(string.Format("Downloading {0}  {1:yyyy-MM-dd}...", entry.Instrument.FullName, entry.Date)));

			try
			{
				object     hdsClient     = null;
				MethodInfo requestMethod = null;

				// Approach 1: ClientConnection.HistoricalDataClient
				if (Connection.ClientConnection != null)
				{
					var prop = Connection.ClientConnection.GetType().GetProperty("HistoricalDataClient",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (prop != null)
						hdsClient = prop.GetValue(Connection.ClientConnection);

					// Fallback: ClientConnection.Adapter
					if (hdsClient == null)
					{
						var adapterProp = Connection.ClientConnection.GetType().GetProperty("Adapter",
							BindingFlags.Public | BindingFlags.Instance);
						if (adapterProp != null)
						{
							var adapter = adapterProp.GetValue(Connection.ClientConnection);
							if (adapter != null)
							{
								requestMethod = adapter.GetType().GetMethod("RequestMarketReplay",
									BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
								if (requestMethod != null) hdsClient = adapter;
							}
						}
					}
				}

				// Approach 2: Search all connections
				if (hdsClient == null && Connection.Connections != null)
				{
					foreach (Connection conn in Connection.Connections)
					{
						if (conn == null) continue;
						var prop = conn.GetType().GetProperty("HistoricalDataClient",
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (prop != null)
						{
							var client = prop.GetValue(conn);
							if (client != null) { hdsClient = client; break; }
						}
					}
				}

				if (hdsClient != null && requestMethod == null)
					requestMethod = hdsClient.GetType().GetMethod("RequestMarketReplay",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

				if (hdsClient == null || requestMethod == null)
				{
					Dispatcher.InvokeAsync(() =>
					{
						LogMessage("ERROR: Market Replay download service not available.");
						LogMessage("Ensure you are connected to a data provider (e.g., Tradovate, Continuum).");
						if (Connection.Connections != null)
						{
							LogMessage(string.Format("Active connections: {0}", Connection.Connections.Count));
							foreach (Connection conn in Connection.Connections)
								if (conn != null)
									LogMessage(string.Format("  * {0}: {1}",
										conn.Options != null ? conn.Options.Name : "Unknown", conn.Status));
						}
					});
					CompleteDownloadProcess();
					return;
				}

				Action<ErrorCode, string, object> callback = (errorCode, errorMessage, state) =>
				{
					if (errorCode == ErrorCode.NoError)
					{
						entry.Status = WUDownloadStatus.Completed;
						Dispatcher.InvokeAsync(() =>
							LogMessage(string.Format("  OK: {0}  {1:yyyy-MM-dd}", entry.Instrument.FullName, entry.Date)));
					}
					else if (errorCode == ErrorCode.UserAbort)
					{
						entry.Status = WUDownloadStatus.Canceled;
						Dispatcher.InvokeAsync(() =>
							LogMessage(string.Format("  Canceled: {0}  {1:yyyy-MM-dd}", entry.Instrument.FullName, entry.Date)));
					}
					else
					{
						entry.Status = WUDownloadStatus.Failed;
						Dispatcher.InvokeAsync(() =>
							LogMessage(string.Format("  FAILED: {0}  {1:yyyy-MM-dd} — {2}",
								entry.Instrument.FullName, entry.Date, errorMessage ?? errorCode.ToString())));
					}

					Dispatcher.InvokeAsync(() =>
					{
						completedDays++;
						UpdateProgress();
						currentDownloadIndex++;
						Globals.RandomDispatcher.InvokeAsync(new Action(() => DownloadNextDay()));
					});
				};

				requestMethod.Invoke(hdsClient, new object[] { entry.Instrument, entry.Date, callback, null, null });
			}
			catch (Exception ex)
			{
				entry.Status = WUDownloadStatus.Failed;
				Dispatcher.InvokeAsync(() =>
				{
					LogMessage(string.Format("ERROR: {0}  {1:yyyy-MM-dd} — {2}",
						entry.Instrument.FullName, entry.Date,
						ex.InnerException != null ? ex.InnerException.Message : ex.Message));
					completedDays++;
					UpdateProgress();
					currentDownloadIndex++;
					Globals.RandomDispatcher.InvokeAsync(new Action(() => DownloadNextDay()));
				});
			}
		}

		private void UpdateProgress()
		{
			pbProgress.Value = completedDays;

			string eta = "";
			if (completedDays > 0 && completedDays < totalDays)
			{
				TimeSpan elapsed = DateTime.Now - startTimestamp;
				double avgSec = elapsed.TotalSeconds / completedDays;
				TimeSpan remaining = TimeSpan.FromSeconds(avgSec * (totalDays - completedDays));
				eta = string.Format("  ETA {0:hh\\:mm\\:ss}", remaining);
			}

			lProgress.Content = string.Format("{0} / {1}{2}", completedDays, totalDays, eta);
		}

		private void CompleteDownloadProcess()
		{
			Dispatcher.InvokeAsync(() =>
			{
				isRunning = false;
				btnDownload.Content   = "_Download";
				btnDownload.IsEnabled = true;
				chkSkipExisting.IsEnabled = true;

				int completed = downloadQueue.Count(d => d.Status == WUDownloadStatus.Completed);
				int failed    = downloadQueue.Count(d => d.Status == WUDownloadStatus.Failed);
				int canceled  = downloadQueue.Count(d => d.Status == WUDownloadStatus.Canceled || d.Status == WUDownloadStatus.Pending);

				LogMessage(isCanceling
					? string.Format("Canceled.  Completed: {0}  Failed: {1}  Canceled: {2}", completed, failed, canceled)
					: string.Format("Done!  Completed: {0}  Failed: {1}", completed, failed));
				LogMessage("-------------------------------------------");
				isCanceling = false;
			});
		}

		private void LogMessage(string text)
		{
			Dispatcher.InvokeAsync(() =>
			{
				tbOutput.AppendText(DateTime.Now.ToString("HH:mm:ss") + " - " + text + Environment.NewLine);
				tbOutput.ScrollToEnd();
			});
		}

		private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
		{
			if (isRunning) isCanceling = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			if (btnDownload != null) btnDownload.Click -= BtnDownload_Click;
			if (btnClearLog != null) btnClearLog.Click -= BtnClearLog_Click;
			base.OnClosed(e);
		}

		#region IWorkspacePersistence

		public WorkspaceOptions WorkspaceOptions { get; set; }

		public void Restore(XDocument document, XElement element) { }

		public void Save(XDocument document, XElement element) { }

		#endregion
	}

	public class WUDownloadEntry
	{
		public DateTime   Date       { get; set; }
		public Instrument Instrument { get; set; }
		public string     NrdPath    { get; set; }
		public WUDownloadStatus Status { get; set; }
	}

	public enum WUDownloadStatus
	{
		Pending,
		InProgress,
		Completed,
		Failed,
		Canceled
	}
}
