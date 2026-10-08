#region Using declarations
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Xml.Linq;
using NinjaTrader.Cbi;
using NinjaTrader.Core;
using NinjaTrader.Data;
using NinjaTrader.NinjaScript;
using NinjaTrader.Gui.Tools;
#endregion


// Release Notes - Please update version number on line 136, Caption
//
// Origional Code by emansenpai 
//
// 2026-0103 GreyBeard - Changed PopulateInstruments() to support differing contract dates
// 2026-0214 - International dates fixed by Jack
// 2026-0221 - Removed skip of Sunday data by Greybeard

namespace NinjaTrader.Gui.NinjaScript
{
	public class MultidayReplayDownloader : AddOnBase
	{
		private NTMenuItem menuItem;
		private NTMenuItem existingMenuItemInControlCenter;
		private MultidayReplayDownloaderWindowEN downloaderWindow;

		protected override void OnStateChange()
		{
			if (State == State.SetDefaults)
			{
				Name = "MultidayReplayDownloader";
				Description = "Download multiple days of market replay data at once";
			}
			else if (State == State.Terminated)
			{
				if (downloaderWindow != null)
				{
					downloaderWindow.Dispatcher.InvokeAsync(() =>
					{
						downloaderWindow.Close();
						downloaderWindow = null;
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
				Header = "Multiday Replay Downloader",
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
				if (downloaderWindow == null)
				{
					downloaderWindow = new MultidayReplayDownloaderWindowEN();
					downloaderWindow.Closed += (s, args) => downloaderWindow = null;
					downloaderWindow.Show();
				}
				else
				{
					downloaderWindow.Activate();
				}
			}));
		}
	}

	public class MultidayReplayDownloaderWindowEN : NTWindow, IWorkspacePersistence
	{
		// UI Controls
		private ComboBox cbInstrument;
		private TextBox tbStartDate;
		private TextBox tbEndDate;
		private Button btnStartDatePicker;
		private Button btnEndDatePicker;
		private Popup popupCalendar;
		private System.Windows.Controls.Calendar calendarControl;
		private bool isStartDatePicker; // true = editing start date, false = editing end date
		private Button btnClearLog;
		private Button btnLast7Days;
		private Button btnLast30Days;
		private Button btnLast90Days;
		private Button btnDownload;
		private CheckBox chkSkipExisting;
		private TextBox tbOutput;
		private Label lProgress;
		private ProgressBar pbProgress;

		// State
		private Instrument selectedInstrument;
		private bool isRunning = false;
		private bool isCanceling = false;
		private int completedDays = 0;
		private int totalDays = 0;
		private DateTime startTimestamp;
		private List<DownloadEntry> downloadQueue;
		private int currentDownloadIndex = 0;

		public MultidayReplayDownloaderWindowEN()
		{
			Caption = "Multiday Replay Downloader v1.3";
			Width = 600;
			Height = 520;
			Content = BuildContent();

			Loaded += (o, e) =>
			{
				if (WorkspaceOptions == null)
					WorkspaceOptions = new WorkspaceOptions("MultidayReplayDownloader-" + Guid.NewGuid().ToString("N"), this);
			};

			Closing += OnWindowClosing;
		}

		private DependencyObject BuildContent()
		{
			// Unified styling constants
			const double margin = 10;
			const double spacing = 8;
			const double labelWidth = 66;
			const double buttonHeight = 26;
			const double inputHeight = 26;
			const double buttonMinWidth = 80;
			const double instrumentInputWidth = 110; // 220 / 2 = 110
			const double datePickerButtonWidth = 25; // Width for two Chinese characters (参考TradingPanelCN.cs)
			const double dateInputWidth = 97; // Date input box width (can be adjusted independently)
			const double tightSpacing = 2; // Reduced spacing for tighter layout
			const double fontSize = 12;

			Grid mainGrid = new Grid { Background = new SolidColorBrush(Colors.Transparent) };
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 0: Instructions
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 1: Instrument
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 2: Start Date
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 3: End Date
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 4: Quick select
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 5: Options
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 6: Download button
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // Row 7: Status log
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 8: Progress label
			mainGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Row 9: Progress bar

			// Common brush for labels
			Brush labelBrush = TryFindResource("FontLabelBrush") as Brush ?? Brushes.White;

			// Row 0: Instructions 
			Label lblInstructions = new Label
			{
				Content = "Select or type in futures instrument and click Load. Then, select dates and click Download.",
				Margin = new Thickness(0, margin, margin, spacing),
				Foreground = labelBrush,
				FontSize = fontSize
			};
			Grid.SetRow(lblInstructions, 0);
			mainGrid.Children.Add(lblInstructions);
			
			// Row 1: Instrument Selection
			Grid instrumentGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing)
			};
			instrumentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
			instrumentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // ComboBox
			instrumentGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Button

			Label lblInstrument = new Label
			{
				Content = "Instrument：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblInstrument, 0);
			instrumentGrid.Children.Add(lblInstrument);

			cbInstrument = new ComboBox
			{
				Width = 98,
				Height = inputHeight,
				IsEditable = true,
				IsTextSearchEnabled = true,
				Margin = new Thickness(0, 0, spacing, 0),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				FontSize = fontSize,
				FontFamily = SystemFonts.MessageFontFamily,
				HorizontalAlignment = HorizontalAlignment.Left
			};
			PopulateInstruments();
			Grid.SetColumn(cbInstrument, 1);
			instrumentGrid.Children.Add(cbInstrument);

			Button btnLoad = new Button
			{
				Content = "Load",
				Height = buttonHeight,
				Width = buttonMinWidth,
				Padding = new Thickness(8, 0, 8, 0),
				FontSize = fontSize
			};
			btnLoad.Click += BtnLoad_Click;
			Grid.SetColumn(btnLoad, 2);
			instrumentGrid.Children.Add(btnLoad);

			Grid.SetRow(instrumentGrid, 1);
			mainGrid.Children.Add(instrumentGrid);

			// Row 2: Start Date Picker
			Grid startDateGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing)
			};
			startDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Start Date Label
			startDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Start Date Input
			startDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Start Date Picker

			Label lblStart = new Label
			{
				Content = "Start Date：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblStart, 0);
			startDateGrid.Children.Add(lblStart);

			tbStartDate = new TextBox
			{
				Text = DateTime.Today.AddDays(-7).ToString("M/d/yyyy"),
				Width = dateInputWidth, // Can be adjusted independently (line 157)
				Height = inputHeight,
				Margin = new Thickness(0, 0, spacing, 0),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				FontSize = fontSize,
				FontFamily = SystemFonts.MessageFontFamily,
				TextAlignment = TextAlignment.Center
			};
			Grid.SetColumn(tbStartDate, 1);
			startDateGrid.Children.Add(tbStartDate);

			btnStartDatePicker = new Button
			{
				Content = "...",
				MinWidth = datePickerButtonWidth,
				Height = buttonHeight,
				Padding = new Thickness(1),
				Margin = new Thickness(0, 0, 0, 0),
				ToolTip = "Open calendar",
				FontSize = fontSize,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center
			};
			btnStartDatePicker.Click += BtnStartDatePicker_Click;
			Grid.SetColumn(btnStartDatePicker, 2);
			startDateGrid.Children.Add(btnStartDatePicker);

			Grid.SetRow(startDateGrid, 2);
			mainGrid.Children.Add(startDateGrid);

			// Row 3: End Date Picker
			Grid endDateGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing)
			};
			endDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // End Date Label
			endDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // End Date Input
			endDateGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // End Date Picker

			Label lblEnd = new Label
			{
				Content = "End Date：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblEnd, 0);
			endDateGrid.Children.Add(lblEnd);

			tbEndDate = new TextBox
			{
				Text = DateTime.Today.ToString("M/d/yyyy"),
				Width = dateInputWidth, // Can be adjusted independently (line 157)
				Height = inputHeight,
				Margin = new Thickness(0, 0, spacing, 0),
				VerticalContentAlignment = VerticalAlignment.Center,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				FontSize = fontSize,
				FontFamily = SystemFonts.MessageFontFamily,
				TextAlignment = TextAlignment.Center
			};
			Grid.SetColumn(tbEndDate, 1);
			endDateGrid.Children.Add(tbEndDate);

			btnEndDatePicker = new Button
			{
				Content = "...",
				MinWidth = datePickerButtonWidth,
				Height = buttonHeight,
				Padding = new Thickness(1),
				Margin = new Thickness(0, 0, 0, 0),
				ToolTip = "Open calendar",
				FontSize = fontSize,
				HorizontalContentAlignment = HorizontalAlignment.Center,
				VerticalContentAlignment = VerticalAlignment.Center
			};
			btnEndDatePicker.Click += BtnEndDatePicker_Click;
			Grid.SetColumn(btnEndDatePicker, 2);
			endDateGrid.Children.Add(btnEndDatePicker);

			// Create shared calendar popup
			calendarControl = new System.Windows.Controls.Calendar
			{
				DisplayDateEnd = DateTime.Today
			};
			calendarControl.SelectedDatesChanged += CalendarControl_SelectedDatesChanged;

			popupCalendar = new Popup
			{
				Child = new Border
				{
					Background = TryFindResource("BackgroundBrush") as Brush ?? new SolidColorBrush(Colors.White),
					BorderBrush = new SolidColorBrush(Colors.Gray),
					BorderThickness = new Thickness(1),
					Child = calendarControl
				},
				StaysOpen = false,
				Placement = PlacementMode.Bottom
			};
			endDateGrid.Children.Add(popupCalendar);

			Grid.SetRow(endDateGrid, 3);
			mainGrid.Children.Add(endDateGrid);

			// Row 4: Quick Select Buttons
			Grid quickSelectGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing)
			};
			quickSelectGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
			quickSelectGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Buttons

			Label lblQuickSelect = new Label
			{
				Content = "Quick Pick：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblQuickSelect, 0);
			quickSelectGrid.Children.Add(lblQuickSelect);

			StackPanel quickPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left
			};

			btnLast7Days = new Button
			{
				Content = "Last 7 Days",
				Height = buttonHeight,
				Width = buttonMinWidth,
				Padding = new Thickness(8, 0, 8, 0),
				Margin = new Thickness(0, 0, spacing, 0),
				FontSize = fontSize
			};
			btnLast7Days.Click += BtnLast7Days_Click;

			btnLast30Days = new Button
			{
				Content = "Last 30 Days",
				Height = buttonHeight,
				Width = buttonMinWidth,
				Padding = new Thickness(8, 0, 8, 0),
				Margin = new Thickness(0, 0, spacing, 0),
				FontSize = fontSize
			};
			btnLast30Days.Click += BtnLast30Days_Click;

			btnLast90Days = new Button
			{
				Content = "Last 90 Days",
				Height = buttonHeight,
				Width = buttonMinWidth,
				Padding = new Thickness(8, 0, 8, 0),
				FontSize = fontSize
			};
			btnLast90Days.Click += BtnLast90Days_Click;

			quickPanel.Children.Add(btnLast7Days);
			quickPanel.Children.Add(btnLast30Days);
			quickPanel.Children.Add(btnLast90Days);
			Grid.SetColumn(quickPanel, 1);
			quickSelectGrid.Children.Add(quickPanel);
			Grid.SetRow(quickSelectGrid, 4);
			mainGrid.Children.Add(quickSelectGrid);

			// Row 5: Options
			StackPanel optionsPanel = new StackPanel
			{
				Orientation = Orientation.Horizontal,
				HorizontalAlignment = HorizontalAlignment.Left,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(margin, 0, margin, spacing)
			};

			chkSkipExisting = new CheckBox
			{
				IsChecked = true,
				VerticalAlignment = VerticalAlignment.Center,
				Margin = new Thickness(0, 0, 4, 0)
			};

			Label lblSkipExisting = new Label
			{
				Content = "Skip existing replay files",
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

			Grid.SetRow(optionsPanel, 5);
			mainGrid.Children.Add(optionsPanel);

			// Row 6: Download Button
			Grid downloadGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing)
			};
			downloadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
			downloadGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Button

			Label lblDownload = new Label
			{
				Content = "Execute：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				HorizontalAlignment = HorizontalAlignment.Left,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, 0, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblDownload, 0);
			downloadGrid.Children.Add(lblDownload);

			btnDownload = new Button
			{
				Content = "_Download",
				IsDefault = true,
				Height = buttonHeight,
				Width = buttonMinWidth,
				Padding = new Thickness(8, 0, 8, 0),
				FontSize = fontSize
			};
			btnDownload.Click += BtnDownload_Click;
			Grid.SetColumn(btnDownload, 1);
			downloadGrid.Children.Add(btnDownload);
			Grid.SetRow(downloadGrid, 6);
			mainGrid.Children.Add(downloadGrid);

			// Row 6: Status Log
			Grid logHeaderGrid = new Grid
			{
				Margin = new Thickness(margin, 0, margin, spacing / 2)
			};
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Label
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // Button (right after label)
			logHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }); // Spacer (fills remaining space)

			Label lblLog = new Label
			{
				Content = "Status Log：",
				Width = labelWidth,
				VerticalAlignment = VerticalAlignment.Center,
				Foreground = labelBrush,
				FontSize = fontSize,
				Margin = new Thickness(0, 0, tightSpacing, 0),
				Padding = new Thickness(0),
				HorizontalContentAlignment = HorizontalAlignment.Left
			};
			Grid.SetColumn(lblLog, 0);
			logHeaderGrid.Children.Add(lblLog);

			btnClearLog = new Button
			{
				Content = "Clear",
				Height = buttonHeight,
				Width = buttonMinWidth,
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
				MinHeight = 150,
				MaxHeight = 300,
				TextWrapping = TextWrapping.Wrap,
				FontSize = fontSize,
				FontFamily = new FontFamily("Consolas")
			};

			Grid logPanel = new Grid();
			logPanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
			logPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
			Grid.SetRow(logHeaderGrid, 0);
			Grid.SetRow(tbOutput, 1);
			logPanel.Children.Add(logHeaderGrid);
			logPanel.Children.Add(tbOutput);
			Grid.SetRow(logPanel, 7);
			mainGrid.Children.Add(logPanel);

			// Row 8: Progress Label
			lProgress = new Label
			{
				Content = "",
				Margin = new Thickness(margin, 0, margin, spacing / 2),
				Height = 0,
				Foreground = labelBrush,
				FontSize = fontSize
			};
			Grid.SetRow(lProgress, 8);
			mainGrid.Children.Add(lProgress);

			// Row 9: Progress Bar
			pbProgress = new ProgressBar
			{
				Height = 0,
				Margin = new Thickness(margin, 0, margin, margin)
			};
			Grid.SetRow(pbProgress, 9);
			mainGrid.Children.Add(pbProgress);

			return mainGrid;
		}
        // Updated by GreyBeard to support all the different contract schedulues - 3 Jan 2026 ##############################################################
        private void PopulateInstruments()
        {
            cbInstrument.Items.Clear();

            // Instrument symbol -> valid contract months (2-digit strings)
			// Shows in in pull down but user can type over if needed
            var commonFutures = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
			{
				// Index futures
				{ "ES",  new[] { "03", "06", "09", "12" } },
				{ "NQ",  new[] { "03", "06", "09", "12" } },
				{ "YM",  new[] { "03", "06", "09", "12" } },
				{ "RTY", new[] { "03", "06", "09", "12" } },

				// Micro index futures
				{ "MES", new[] { "03", "06", "09", "12" } },
				{ "MNQ", new[] { "03", "06", "09", "12" } },
				{ "MYM", new[] { "03", "06", "09", "12" } },
				{ "M2K", new[] { "03", "06", "09", "12" } },

				// Commodities
				{ "CL",  new[] { "01", "02", "03", "04", "05", "06", "07", "08", "09", "10", "11", "12" } },
				{ "GC",  new[] { "02", "04", "06", "08", "10", "12" } },
				{ "SI",  new[] { "03", "05", "07", "09", "12" } },

				// Currencies
				{ "6E",  new[] { "03", "06", "09", "12" } },
				{ "6J",  new[] { "03", "06", "09", "12" } },
				{ "6B",  new[] { "03", "06", "09", "12" } },

				// Treasuries
				{ "ZB",  new[] { "03", "06", "09", "12" } },
				{ "ZN",  new[] { "03", "06", "09", "12" } },
				{ "ZF",  new[] { "03", "06", "09", "12" } },
			};

            int currentYear = DateTime.Now.Year;
            int currentMonth = DateTime.Now.Month;

            foreach (var kvp in commonFutures)
            {
                string root = kvp.Key;
                string[] months = kvp.Value;

                string contractSuffix = null;

                // Special case: CL expires ~20th of prior month, so active contract is current month + 2
		        if (root.Equals("CL", StringComparison.OrdinalIgnoreCase))
		        {
		            int nextMonth = currentMonth + 2;
		            int year = currentYear;

		            if (nextMonth > 12)
		            {
		                nextMonth -= 12;
		                year++;
		            }

		            contractSuffix = string.Format("{0:D2}-{1:D2}", nextMonth, year % 100);
		        }
		        else
		        {
		            foreach (var m in months)
		            {
		                if (int.Parse(m) >= currentMonth)
		                {
		                    contractSuffix = string.Format("{0}-{1:D2}", m, currentYear % 100);
		                    break;
		                }
		            }
		
		            // Wrap to next year if needed
		            if (contractSuffix == null)
		                contractSuffix = string.Format("{0}-{1:D2}", months[0], (currentYear + 1) % 100);
		        }

                cbInstrument.Items.Add(string.Format("{0} {1}", root, contractSuffix));
            }

            // Select first item by default
            if (cbInstrument.Items.Count > 0)
                cbInstrument.SelectedIndex = 0;
        }




        private string MonthCodeToNumber(string monthCode)
		{
			// Convert month code (H, M, U, Z, etc.) to month number format (03, 06, 09, 12)
			switch (monthCode.ToUpper())
			{
				case "F": return "01";
				case "G": return "02";
				case "H": return "03";
				case "J": return "04";
				case "K": return "05";
				case "M": return "06";
				case "N": return "07";
				case "Q": return "08";
				case "U": return "09";
				case "V": return "10";
				case "X": return "11";
				case "Z": return "12";
				default: return monthCode;
			}
		}

		private void BtnLoad_Click(object sender, RoutedEventArgs e)
		{
			string instrumentName = cbInstrument.Text.Trim();
			if (string.IsNullOrEmpty(instrumentName))
			{
				LogMessage("Please enter an instrument name.");
				return;
			}

			// Normalize the instrument name (handle both "NQ H26" and "NQ 03-26" formats)
			instrumentName = NormalizeInstrumentName(instrumentName);

			try
			{
				selectedInstrument = Instrument.GetInstrument(instrumentName);
				if (selectedInstrument == null)
				{
					LogMessage(string.Format("ERROR: Instrument '{0}' not found. Please check the name.", instrumentName));
				}
				else
				{
					LogMessage(string.Format("Loaded instrument: {0}", selectedInstrument.FullName));
					// Update the combobox with the normalized/full name
					cbInstrument.Text = selectedInstrument.FullName;
				}
			}
			catch (Exception ex)
			{
				LogMessage(string.Format("ERROR: Failed to load instrument: {0}", ex.Message));
			}
		}

		private string NormalizeInstrumentName(string input)
		{
			// Handle formats like "NQ H26" -> "NQ 03-26" or "ES Z25" -> "ES 12-25"
			// Also handle "NQH26" -> "NQ 03-26"
			if (string.IsNullOrEmpty(input))
				return input;

			input = input.Trim().ToUpper();

			// Pattern: SYMBOL + optional space + MONTHCODE + YEAR (e.g., "NQ H26", "NQH26", "ES Z25")
			// Month codes: F,G,H,J,K,M,N,Q,U,V,X,Z
			Regex shortFormatRegex = new Regex(@"^([A-Z0-9]+)\s*([FGHJKMNQUVXZ])(\d{2})$");

			Match match = shortFormatRegex.Match(input);
			if (match.Success)
			{
				string symbol = match.Groups[1].Value;
				string monthCode = match.Groups[2].Value;
				string year = match.Groups[3].Value;

				string monthNumber = MonthCodeToNumber(monthCode);
				return string.Format("{0} {1}-{2}", symbol, monthNumber, year);
			}

			// Already in correct format or unknown format - return as-is
			return input;
		}

		private void BtnLast7Days_Click(object sender, RoutedEventArgs e)
		{
			tbStartDate.Text = DateTime.Today.AddDays(-7).ToString("M/d/yyyy");
			tbEndDate.Text = DateTime.Today.ToString("M/d/yyyy");
		}

		private void BtnLast30Days_Click(object sender, RoutedEventArgs e)
		{
			tbStartDate.Text = DateTime.Today.AddDays(-30).ToString("M/d/yyyy");
			tbEndDate.Text = DateTime.Today.ToString("M/d/yyyy");
		}

		private void BtnLast90Days_Click(object sender, RoutedEventArgs e)
		{
			tbStartDate.Text = DateTime.Today.AddDays(-90).ToString("M/d/yyyy");
			tbEndDate.Text = DateTime.Today.ToString("M/d/yyyy");
		}

		private void BtnStartDatePicker_Click(object sender, RoutedEventArgs e)
		{
			isStartDatePicker = true;
			DateTime currentDate;
			if (DateTime.TryParseExact(tbStartDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out currentDate))
				calendarControl.SelectedDate = currentDate;
			popupCalendar.PlacementTarget = btnStartDatePicker;
			popupCalendar.IsOpen = true;
		}

		private void BtnEndDatePicker_Click(object sender, RoutedEventArgs e)
		{
			isStartDatePicker = false;
			DateTime currentDate;
			if (DateTime.TryParseExact(tbEndDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out currentDate))
				calendarControl.SelectedDate = currentDate;
			popupCalendar.PlacementTarget = btnEndDatePicker;
			popupCalendar.IsOpen = true;
		}

		private void CalendarControl_SelectedDatesChanged(object sender, SelectionChangedEventArgs e)
		{
			if (calendarControl.SelectedDate.HasValue)
			{
				string dateStr = calendarControl.SelectedDate.Value.ToString("M/d/yyyy");
				if (isStartDatePicker)
					tbStartDate.Text = dateStr;
				else
					tbEndDate.Text = dateStr;
				popupCalendar.IsOpen = false;
			}
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
				// Handle cancel request
				if (!isCanceling)
				{
					isCanceling = true;
					LogMessage("Canceling download...");
					btnDownload.IsEnabled = false;
					btnDownload.Content = "Canceling...";
				}
				return;
			}

			// Validate inputs
			if (!ValidateInputs())
				return;

			// Build download queue
			BuildDownloadQueue();

			if (downloadQueue.Count == 0)
			{
				LogMessage("No days to download (all existing or invalid date range).");
				return;
			}

			// Start download
			StartDownloadProcess();
		}

		private bool ValidateInputs()
		{
			// Check instrument
			if (selectedInstrument == null)
			{
				string instrumentName = cbInstrument.Text.Trim();
				if (string.IsNullOrEmpty(instrumentName))
				{
					LogMessage("ERROR: Please enter an instrument name and click Load.");
					return false;
				}

				try
				{
					selectedInstrument = Instrument.GetInstrument(instrumentName);
					if (selectedInstrument == null)
					{
						LogMessage(string.Format("ERROR: Instrument '{0}' not found.", instrumentName));
						return false;
					}
				}
				catch (Exception ex)
				{
					LogMessage(string.Format("ERROR: Failed to load instrument: {0}", ex.Message));
					return false;
				}
			}

			// Check dates
			DateTime startDate, endDate;
			if (!DateTime.TryParseExact(tbStartDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out startDate))
			{
				LogMessage("ERROR: Invalid start date format. Please use M/d/yyyy format.");
				return false;
			}

			if (!DateTime.TryParseExact(tbEndDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out endDate))
			{
				LogMessage("ERROR: Invalid end date format. Please use M/d/yyyy format.");
				return false;
			}

			if (startDate > endDate)
			{
				LogMessage("ERROR: Start date must be before or equal to end date.");
				return false;
			}

			if (endDate > DateTime.Today)
			{
				LogMessage("ERROR: End date cannot be in the future.");
				return false;
			}

			return true;
		}

		private void BuildDownloadQueue()
		{
			downloadQueue = new List<DownloadEntry>();
			DateTime startDate = DateTime.ParseExact(tbStartDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture);
			DateTime endDate = DateTime.ParseExact(tbEndDate.Text, "M/d/yyyy", CultureInfo.InvariantCulture);

			string replayDir = Path.Combine(Core.Globals.UserDataDir, "db", "replay", selectedInstrument.FullName);

			int skippedWeekends = 0;
			int skippedExisting = 0;

			for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
			{
                // Skip weekends (Saturday=6, Sunday=0)
                //Changed to allow SUnday eving data - GreyBeard 
                //removed || date.DayOfWeek == DayOfWeek.Sunday
                if (date.DayOfWeek == DayOfWeek.Saturday )
				{
					skippedWeekends++;
					continue;
				}

				string nrdFileName = date.ToString("yyyyMMdd") + ".nrd";
				string nrdFilePath = Path.Combine(replayDir, nrdFileName);

				// Check if file exists (skip existing if checkbox checked)
				if (chkSkipExisting.IsChecked == true && File.Exists(nrdFilePath))
				{
					skippedExisting++;
					continue;
				}

				downloadQueue.Add(new DownloadEntry
				{
					Date = date,
					Instrument = selectedInstrument,
					NrdFilePath = nrdFilePath,
					Status = DownloadStatus.Pending
				});
			}

			if (skippedWeekends > 0)
				LogMessage(string.Format("Skipped {0} Saturdays.", skippedWeekends));  //Saturday only skip
			if (skippedExisting > 0)
				LogMessage(string.Format("Skipped {0} day(s) with existing replay files.", skippedExisting));

			totalDays = downloadQueue.Count;
			completedDays = 0;
			currentDownloadIndex = 0;
		}

		private void StartDownloadProcess()
		{
			isRunning = true;
			isCanceling = false;
			startTimestamp = DateTime.Now;

			// Update UI
			btnDownload.Content = "_Cancel";
			cbInstrument.IsEnabled = false;
			tbStartDate.IsEnabled = false;
			tbEndDate.IsEnabled = false;
			btnStartDatePicker.IsEnabled = false;
			btnEndDatePicker.IsEnabled = false;
			btnLast7Days.IsEnabled = false;
			btnLast30Days.IsEnabled = false;
			btnLast90Days.IsEnabled = false;
			chkSkipExisting.IsEnabled = false;

			// Setup progress bar
			double margin = 8;
			pbProgress.Minimum = 0;
			pbProgress.Maximum = totalDays;
			pbProgress.Value = 0;
			pbProgress.Height = 16;
			pbProgress.Margin = new Thickness(margin, 0, margin, margin);
			lProgress.Height = 24;

			LogMessage(string.Format("Starting download of {0} day(s) for {1}...", totalDays, selectedInstrument.FullName));

			// Start first download (sequential processing)
			Globals.RandomDispatcher.InvokeAsync(new Action(() => DownloadNextDay()));
		}

		private void DownloadNextDay()
		{
			if (isCanceling || currentDownloadIndex >= downloadQueue.Count)
			{
				CompleteDownloadProcess();
				return;
			}

			DownloadEntry entry = downloadQueue[currentDownloadIndex];
			entry.Status = DownloadStatus.InProgress;

			Dispatcher.InvokeAsync(() =>
			{
				LogMessage(string.Format("Downloading {0} for {1:yyyy-MM-dd}...", entry.Instrument.FullName, entry.Date));
			});

			try
			{
				// Try multiple approaches to find HdsClient/ClientAdapter for market replay download
				object hdsClient = null;
				MethodInfo requestMethod = null;

				// Approach 1: Try Connection.ClientConnection (NinjaTrader's internal connection)
				if (Connection.ClientConnection != null)
				{
					// First try HistoricalDataClient property
					var hdsClientProp = Connection.ClientConnection.GetType().GetProperty("HistoricalDataClient",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
					if (hdsClientProp != null)
					{
						hdsClient = hdsClientProp.GetValue(Connection.ClientConnection);
					}

					// If no HdsClient, try Adapter (ClientAdapter has RequestMarketReplay)
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
								if (requestMethod != null)
								{
									hdsClient = adapter;
								}
							}
						}
					}
				}

				// Approach 2: Search through all connections for one with HistoricalDataClient
				if (hdsClient == null && Connection.Connections != null)
				{
					foreach (Connection conn in Connection.Connections)
					{
						if (conn == null) continue;

						var hdsClientProp = conn.GetType().GetProperty("HistoricalDataClient",
							BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						if (hdsClientProp != null)
						{
							var client = hdsClientProp.GetValue(conn);
							if (client != null)
							{
								hdsClient = client;
								Dispatcher.InvokeAsync(() =>
								{
									LogMessage(string.Format("Using HDS from connection: {0}", (conn.Options != null && conn.Options.Name != null ? conn.Options.Name : "Unknown")));
								});
								break;
							}
						}
					}
				}

				// Get the RequestMarketReplay method if we found an HdsClient but don't have method yet
				if (hdsClient != null && requestMethod == null)
				{
					requestMethod = hdsClient.GetType().GetMethod("RequestMarketReplay",
						BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				}

				// Check if we have what we need
				if (hdsClient == null || requestMethod == null)
				{
					Dispatcher.InvokeAsync(() =>
					{
						LogMessage("ERROR: Could not access Market Replay download service.");
						LogMessage("");
						LogMessage("To download Market Replay data, you need an active data connection");
						LogMessage("that supports historical data. Please ensure:");
						LogMessage("  1. You are connected to a data provider (e.g., Tradovate, Continuum)");
						LogMessage("  2. Your data connection is active and logged in");
						LogMessage("");
						LogMessage("Diagnostic info:");
						LogMessage("  - ClientConnection: " + (Connection.ClientConnection != null ? "Available" : "NULL"));
						// AuthenticatedUser may not be available in all NinjaTrader versions
						// LogMessage("  - AuthenticatedUser: " + (Globals.AuthenticatedUser != null ? (Globals.AuthenticatedUser.UserName != null ? Globals.AuthenticatedUser.UserName : "No username") : "NULL"));
						if (Connection.Connections != null)
						{
							LogMessage(string.Format("  - Active Connections: {0}", Connection.Connections.Count));
							foreach (Connection conn in Connection.Connections)
							{
								if (conn != null)
									LogMessage(string.Format("    * {0}: {1}", (conn.Options != null && conn.Options.Name != null ? conn.Options.Name : "Unknown"), conn.Status));
							}
						}
					});
					CompleteDownloadProcess();
					return;
				}

				// Create the callback delegate: Action<ErrorCode, string, object>
				Action<ErrorCode, string, object> downloadCallback = (errorCode, errorMessage, state) =>
				{
					if (errorCode == ErrorCode.NoError)
					{
						entry.Status = DownloadStatus.Completed;
						Dispatcher.InvokeAsync(() =>
						{
							LogMessage(string.Format("Completed: {0:yyyy-MM-dd}", entry.Date));
						});
					}
					else if (errorCode == ErrorCode.UserAbort)
					{
						entry.Status = DownloadStatus.Canceled;
						Dispatcher.InvokeAsync(() =>
						{
							LogMessage(string.Format("Canceled: {0:yyyy-MM-dd}", entry.Date));
						});
					}
					else
					{
						entry.Status = DownloadStatus.Failed;
						Dispatcher.InvokeAsync(() =>
						{
							LogMessage(string.Format("Failed: {0:yyyy-MM-dd} - {1}", entry.Date, errorMessage ?? errorCode.ToString()));
						});
					}

					// Update progress and process next day
					Dispatcher.InvokeAsync(() =>
					{
						completedDays++;
						UpdateProgress();

						// Process next day
						currentDownloadIndex++;
						Globals.RandomDispatcher.InvokeAsync(new Action(() => DownloadNextDay()));
					});
				};

				// Invoke RequestMarketReplay: (Instrument, DateTime dateEst, Action<ErrorCode,string,object>, IProgress, object state)
				// Note: dateEst should be the date in Eastern Time
				requestMethod.Invoke(hdsClient, new object[] { entry.Instrument, entry.Date, downloadCallback, null, null });
			}
			catch (Exception ex)
			{
				entry.Status = DownloadStatus.Failed;
				Dispatcher.InvokeAsync(() =>
				{
					LogMessage(string.Format("ERROR: {0:yyyy-MM-dd} - {1}", entry.Date, (ex.InnerException != null && ex.InnerException.Message != null ? ex.InnerException.Message : ex.Message)));
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
				double avgTimePerDay = elapsed.TotalSeconds / completedDays;
				int remainingDays = totalDays - completedDays;
				TimeSpan remaining = TimeSpan.FromSeconds(avgTimePerDay * remainingDays);
				eta = string.Format(" ETA: {0:hh\\:mm\\:ss}", remaining);
			}

			lProgress.Content = string.Format("{0} of {1} day(s) processed{2}", completedDays, totalDays, eta);
		}

		private void CompleteDownloadProcess()
		{
			Dispatcher.InvokeAsync(() =>
			{
				isRunning = false;

				// Restore UI
				btnDownload.Content = "_Download";
				btnDownload.IsEnabled = true;
				cbInstrument.IsEnabled = true;
				tbStartDate.IsEnabled = true;
				tbEndDate.IsEnabled = true;
				btnStartDatePicker.IsEnabled = true;
				btnEndDatePicker.IsEnabled = true;
				btnLast7Days.IsEnabled = true;
				btnLast30Days.IsEnabled = true;
				btnLast90Days.IsEnabled = true;
				chkSkipExisting.IsEnabled = true;

				// Summary
				int completed = downloadQueue.Count(d => d.Status == DownloadStatus.Completed);
				int failed = downloadQueue.Count(d => d.Status == DownloadStatus.Failed);
				int canceled = downloadQueue.Count(d => d.Status == DownloadStatus.Canceled || d.Status == DownloadStatus.Pending);

				string summary;
				if (isCanceling)
				{
					summary = string.Format("Download canceled. Completed: {0}, Failed: {1}, Canceled: {2}", completed, failed, canceled);
				}
				else
				{
					summary = string.Format("Download complete! Completed: {0}, Failed: {1}", completed, failed);
				}

				LogMessage(summary);
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
			// Signal cancellation
			if (isRunning)
				isCanceling = true;
		}

		protected override void OnClosed(EventArgs e)
		{
			// Unsubscribe all event handlers
			if (btnDownload != null) btnDownload.Click -= BtnDownload_Click;
			if (btnLast7Days != null) btnLast7Days.Click -= BtnLast7Days_Click;
			if (btnLast30Days != null) btnLast30Days.Click -= BtnLast30Days_Click;
			if (btnLast90Days != null) btnLast90Days.Click -= BtnLast90Days_Click;
			if (btnClearLog != null) btnClearLog.Click -= BtnClearLog_Click;
			if (btnStartDatePicker != null) btnStartDatePicker.Click -= BtnStartDatePicker_Click;
			if (btnEndDatePicker != null) btnEndDatePicker.Click -= BtnEndDatePicker_Click;
			if (calendarControl != null) calendarControl.SelectedDatesChanged -= CalendarControl_SelectedDatesChanged;

			base.OnClosed(e);
		}

		#region IWorkspacePersistence

		public WorkspaceOptions WorkspaceOptions { get; set; }

		public void Restore(XDocument document, XElement element)
		{
			foreach (XElement elRoot in element.Elements())
			{
				if (elRoot.Name.LocalName.Contains("MultidayReplayDownloader"))
				{
					XElement elInstrument = elRoot.Element("LastInstrument");
					if (elInstrument != null && cbInstrument != null)
						cbInstrument.Text = elInstrument.Value;

					XElement elSkipExisting = elRoot.Element("SkipExisting");
					if (elSkipExisting != null && chkSkipExisting != null)
					{
						bool skipValue;
						if (bool.TryParse(elSkipExisting.Value, out skipValue))
							chkSkipExisting.IsChecked = skipValue;
					}
				}
			}
		}

		public void Save(XDocument document, XElement element)
		{
			element.Elements().Where(el => el.Name.LocalName.Equals("MultidayReplayDownloader")).Remove();

			XElement elRoot = new XElement("MultidayReplayDownloader");
			elRoot.Add(new XElement("LastInstrument", cbInstrument != null ? cbInstrument.Text : ""));
			elRoot.Add(new XElement("SkipExisting", chkSkipExisting != null ? chkSkipExisting.IsChecked.ToString() : "True"));
			element.Add(elRoot);
		}

		#endregion
	}

	public class DownloadEntry
	{
		public DateTime Date { get; set; }
		public Instrument Instrument { get; set; }
		public string NrdFilePath { get; set; }
		public DownloadStatus Status { get; set; }
	}

	public enum DownloadStatus
	{
		Pending,
		InProgress,
		Completed,
		Failed,
		Canceled
	}
}
