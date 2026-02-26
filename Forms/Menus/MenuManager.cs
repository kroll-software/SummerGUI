using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using KS.Foundation;
using Pfz.Collections;

namespace SummerGUI
{
	public static class MainMenuTags
	{
		public const string SelectAll = "SelectAll";
		public const string InvertSelection = "InvertSelection";
	}		

	public class MenuCommandDefinitionInterface
	{
		public MenuCommandDefinitionInterface()
		{
			Commands = new List<MenuCommandDefinition>();
		}

		public string InterfaceName { get; set; }
		public Type InterfaceType { get; set; }
		public List<MenuCommandDefinition> Commands { get; private set; }
	}

	public class MenuCommandDefinition
	{						
		public IGuiMenuItem MenuItem { get; set; }
		public MethodInfo CommandMethod { get; set; }
		public MethodInfo TestProperty { get; set; }
	}

	public class MenuCommandDefinitionCollection : DisposableObject
	{
		public ThreadSafeDictionary<Type, MenuCommandDefinitionInterface> Interfaces { get; private set; }
		IGuiMenu Menu;

		public MenuCommandDefinitionCollection(IGuiMenu menu) 
		{
			Menu = menu;
			Interfaces = new ThreadSafeDictionary<Type, MenuCommandDefinitionInterface> ();			
			AutoAddInterfaces();
		}

		public void AutoAddInterfaces()
		{			
			ReflectionUtils.FindTypesMarkedByAttributes (new Type[]{ typeof(GuiMenuInterfaceAttribute) }, null)
				.ForEach(AddInterface);
		}

		public void AddInterface(Type interfaceType)
		{
			var menuInterface = new MenuCommandDefinitionInterface();
			menuInterface.InterfaceName = interfaceType.Name;

			menuInterface.InterfaceType = interfaceType;

			//Console.WriteLine (menuInterface.InterfaceName);
			//Console.WriteLine (menuInterface.MessageSubject);			
			foreach (MethodInfo method in interfaceType.GetMethods()) {					
				if (!method.Name.StartsWith("get_")) {																		
					IGuiMenuItem menu = Menu.FindItem(method.Name);
					if (menu != null) {
						MethodInfo propInfo = null;
						string property = String.Format("Can{0}", method.Name);
						PropertyInfo prop = interfaceType.GetProperty (property);
						if (prop != null) {
							propInfo = prop.GetGetMethod ();
						}
						menuInterface.Commands.Add(new MenuCommandDefinition {
							CommandMethod = method,
							TestProperty = propInfo,
							MenuItem = menu,
						});
					} else {
						//this.LogWarning("MenuItem not found: {0}", method.Name);
					}
				}
			}

			//Interfaces.Add(menuInterface.MessageSubject, menuInterface);
			Interfaces.Add(interfaceType, menuInterface);
		}

		protected override void CleanupManagedResources ()
		{
			Menu = null;
			Interfaces.Clear ();
			base.CleanupManagedResources ();
		}		
	}
		
	public class MenuManager : DisposableObject
	{
		public Observer<EventMessage> Observer { get; private set; }
		public MenuCommandDefinitionCollection MenuCommandDefinitions { get; private set; }

		public RootContainer Root { get; private set; }
		public IGuiMenu Menu { get; private set; }

		public MenuManager (RootContainer root)
		{
			Root = root;
			Observer = new Observer<EventMessage> (OnNext, OnError, OnCompleted);		
			root.Subscribe (Observer);
		}			

		public void InitMenu(IGuiMenu menu)
		{
			Menu = menu;

			// Takes 46 milliseconds
			//PerformanceTimer.Time (() => {				
				MenuCommandDefinitions = new MenuCommandDefinitionCollection (Menu);

				foreach (var def in MenuCommandDefinitions.Interfaces) {					
					def.Value.Commands.ForEach (command => {						
						command.MenuItem.Click += delegate {
							try {
								IGuiMenuInterface consumer = Root.FocusedWidget as IGuiMenuInterface;
								if (consumer != null && consumer.HasInterface(def.Value.InterfaceType)) {
									command.CommandMethod.Invoke (consumer, null);
									// in most cases this action requires a menu update now
									// which is not invoked by user input. We have invoked it here
									// we are responsible for an update.
									HandleMenusUpdate (consumer);
								}
							} catch (Exception ex) {
								ex.LogError();
							}
						};
					});
				}
			//});
		}

		public void OnNext(EventMessage message)
		{						
			switch (message.Subject) {
			case RootMessages.FocusChanged:				
				HandleFocusMessage (message.Sender);
				break;

			case RootMessages.UpdateMenus:				
				//HandleInterfaceMessage (message.Sender);
				// both work, this looks more safe--
				// This one also disables all other menu items from the other interfaces
				// which this sender does not implement.
				HandleMenusUpdate (message.Sender);
				break;

			//default: 				
			//	break;
			}
			Root.Invalidate (3);
		}

		void HandleFocusMessage(object sender)
		{			
			MenuCommandDefinitions.Interfaces.Values.ForEach (inter => {
				if (sender.HasInterface(inter.InterfaceType)) {
					inter.Commands.Where(cmd => cmd.TestProperty != null).ForEach (cmd => {
						cmd.MenuItem.Enabled = (bool)cmd.TestProperty.Invoke(sender, null);
					});	
				} else {
					inter.Commands.ForEach (cmd => cmd.MenuItem.Enabled = false);
				}
			});
		}

		/***
		void HandleInterfaceMessage(object sender)
		{								
			if (sender == null)
				return;
			int count = 0;
			foreach (Type iface in sender.GetType().GetInterfaces()) {
				count++;
				MenuCommandDefinitionInterface inter;
				if (MenuCommandDefinitions.Interfaces.TryGetValue (iface, out inter)) {
					inter.Commands.ForEach (cmd => {
						cmd.MenuItem.Enabled = (bool)cmd.TestProperty.Invoke (sender, null);
					});
				}
			}
			//Console.WriteLine ("{0} Interfaces in type", count);
		}
		***/

		void HandleMenusUpdate(object sender)
		{			
			MenuCommandDefinitions.Interfaces.Values.ForEach (inter => {
				if (sender.HasInterface(inter.InterfaceType)) {
					inter.Commands.Where(cmd => cmd.TestProperty != null).ForEach (cmd => {
						cmd.MenuItem.Enabled = (bool)cmd.TestProperty.Invoke(sender, null);
					});	
				} else {
					inter.Commands.ForEach (cmd => cmd.MenuItem.Enabled = false);
				}
			});
		}			
			
		public void OnError(Exception ex)
		{
			Root.ParentWindow.Do(main => main.ShowError (ex));
		}

		public void OnCompleted()
		{
			HandleFocusMessage (null as Widget);
		}

		// *** Automatic ContextMenu Generator ***

		public IGuiMenu GetContextMenuForWidget(Widget w, string widgetname) {
			if (!w.OnSetupContextMenu (widgetname))
				return null;
			IGuiMenu legacy = w.ContextMenu;
			if (!w.AutoContextMenu)
				return legacy;
			IGuiMenu auto = GetAutoContextMenu (w);
			IGuiMenu merged = MergeMenus (legacy, auto);
			if (merged == null || merged.Count == 0)
				return null;
			return merged;
		}

		public IGuiMenu MergeMenus(IGuiMenu menu1, IGuiMenu menu2)
		{						
			if (menu1 == null || menu1.Count == 0)
				return menu2;
			if (menu2 == null || menu2.Count == 0)
				return menu1;			
			GuiMenu m = new GuiMenu (menu1.Name + "+" + menu2.Name);
			int i = 0;
			while (i < menu1.Count)
				m.Add (menu1.Children [i++]);
			if (!m.Last ().IsSeparator)
				m.Add (new GuiMenuItem ("mergeseparator", "-"));			
			i = 0;
			while (i < menu2.Count && menu2.Children [i].IsSeparator)
				i++;
			while (i < menu2.Count) {
				IGuiMenuItem itm = menu2.Children [i++];
				if (itm.IsSeparator) {
					if (!m.Last().IsSeparator)
						m.Add (itm);
				} else if (m.FindItem(itm.Name) == null)
					m.Add (itm);
			}
			while (m.Count > 0 && m.Last ().IsSeparator)
				m.RemoveAt (m.Count - 1);
			return m;
		}

		public IGuiMenu GetAutoContextMenu(Widget w)
		{		
			GuiMenu m = new GuiMenu ("auto");
			int sepCount = 0;

			MenuCommandDefinitions.Interfaces.Values.Where (t => t.Commands.Count > 0 && w.HasInterface (t.InterfaceType))
				.OrderBy (g => g.Commands.First ().MenuItem.Rank)
				.ForEach (g => {
				g.Commands.Select (c => c.MenuItem).OrderBy (x => x.Rank)
						.ForEach (m.Add);
				m.Add (new GuiMenuItem ("sep" + sepCount++, "-"));
			});					

			while (m.Count > 0 && m.Last ().IsSeparator)
				m.RemoveAt (m.Count - 1);			

			return m;
		}

		protected override void CleanupManagedResources ()
		{			
			Observer.Dispose ();
			Menu = null;
			Root = null;

			if (MenuCommandDefinitions != null)
				MenuCommandDefinitions.Dispose ();

			base.CleanupManagedResources ();
		}
	}
}

