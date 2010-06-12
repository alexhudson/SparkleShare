//   SparkleShare, an instant update workflow to Git.
//   Copyright (C) 2010  Hylke Bons <hylkebons@gmail.com>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.

using Gtk;
using Notifications;
using SparkleShare;

namespace SparkleShare {
	
	public class SparkleBubble : Notification {

		public SparkleBubble (string Title, string Subtext) : base (Title, Subtext)  {
				Timeout = 4500;
				Urgency = Urgency.Low;
				IconName = "folder-sparkleshare";
				AttachToStatusIcon (SparkleUI.NotificationIcon);
		}

		// Checks whether the system allows adding buttons to a notification,
		// prevents error messages in Ubuntu.
		new public void AddAction (string Action, string Label,
		                           ActionHandler Handler) {

			bool CanHaveButtons =
				(System.Array.IndexOf (Notifications.Global.Capabilities,
				                       "actions") > -1);
			if (CanHaveButtons)
				base.AddAction(Action, Label, Handler);

		}

	}

}
