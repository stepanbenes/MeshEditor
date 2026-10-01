using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace MeshEditor.AvaloniaUI.Controls
{
	/// <summary>
	/// Access keys ("_Configure" -> C) for the items of a MenuFlyout, which Avalonia handles only in menus and context menus
	/// (WinForms drop-down buttons of tool strips support them).
	/// </summary>
	public static class FlyoutAccessKeys
	{
		public static void Attach(MenuFlyout flyout)
		{
			bool attached = false;
			flyout.Opened += (s, e) =>
			{
				if (attached)
					return;
				if (flyout.Items.OfType<MenuItem>().FirstOrDefault()?.Parent is Control presenter)
				{
					presenter.AddHandler(InputElement.KeyDownEvent, (sender, args) => onKeyDown(flyout, args), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, handledEventsToo: false);
					attached = true;
				}
			};
		}

		private static void onKeyDown(MenuFlyout flyout, KeyEventArgs e)
		{
			if (e.Handled || e.KeyModifiers != KeyModifiers.None && e.KeyModifiers != KeyModifiers.Alt)
				return;
			char? pressed = keyToChar(e.Key);
			if (pressed == null)
				return;

			foreach (var item in flyout.Items.OfType<MenuItem>())
			{
				if (item.IsEffectivelyEnabled && item.IsVisible && AccessKey(item.Header as string) == pressed)
				{
					e.Handled = true;
					flyout.Hide();
					item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent, item));
					return;
				}
			}
		}

		/// <summary>
		/// Access key of a header with Avalonia access text ("Ed_ge" -> 'g'), lower case.
		/// </summary>
		public static char? AccessKey(string header)
		{
			if (string.IsNullOrEmpty(header))
				return null;
			for (int i = 0; i < header.Length - 1; i++)
			{
				if (header[i] == '_')
				{
					if (header[i + 1] == '_')
					{
						i++; // escaped underscore
						continue;
					}
					return char.ToLowerInvariant(header[i + 1]);
				}
			}
			return null;
		}

		private static char? keyToChar(Key key)
		{
			if (key >= Key.A && key <= Key.Z)
				return (char)('a' + (key - Key.A));
			if (key >= Key.D0 && key <= Key.D9)
				return (char)('0' + (key - Key.D0));
			return null;
		}
	}
}
