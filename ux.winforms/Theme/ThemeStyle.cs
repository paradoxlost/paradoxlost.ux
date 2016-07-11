﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Paradoxlost.UX.WinForms.Theme
{
	using StringDictionary = Dictionary<string, string>;
	using StringKeyValue = KeyValuePair<string, string>;

	class ThemeStyle
	{
		protected static Assembly FormsAssembly { get; private set; }
		protected static Module FormsModule { get; private set; }
		static ThemeStyle()
		{
			Type controlType = typeof(Control);
			FormsAssembly = controlType.Assembly;
			FormsModule = controlType.Module;
		}

		public Type TargetClass { get; protected set; }
		internal StringDictionary Variables { get; set; }
		protected StringDictionary Properties { get; set; }

		public ThemeStyle()
		{
			Properties = new StringDictionary();
		}

		public ThemeStyle(string className)
			: this()
		{
			// how to handle namespace resolution??
			// for now assume all types are in System.Windows.Forms
			Type[] targets = FormsModule.FindTypes((t, c) => t.Name == (string)c, className);

			if (targets.Length != 1)
			{
				throw new ArgumentException("Not a valid Control class", "className");
			}

			TargetClass = targets[0];
		}

		public void AddProperty(string name, string value)
		{
			Properties.Add(name, value);
		}

		public void Apply(Control control)
		{
			foreach (StringKeyValue pair in Properties)
			{
				PropertyInfo pi = TargetClass.GetProperty(pair.Key, BindingFlags.Instance | BindingFlags.Public);
				if (pi == null)
					continue;

				Type propertyType = pi.PropertyType;
				string[] args = pair.Value.Split(new string[] { ", " }, StringSplitOptions.None);

				// color is stupid
				// it doesn't use ctors. it has a bunch of static methods
				if (propertyType == typeof(Color))
				{
					object value;
					// we'll only handle names & html
					if (args[0][0] == '#')
					{
						value = ColorTranslator.FromHtml(args[0]);
					}
					else
					{
						value = Color.FromName(args[0]);
					}
					pi.SetValue(control, value);
				}

				// find a property ctor with the same number of args
				foreach (ConstructorInfo ci in propertyType.GetConstructors())
				{
					ParameterInfo[] ctorArgs = ci.GetParameters();
					if (ctorArgs.Length != args.Length)
						continue;

					// see if we can use this one
					object[] values = new object[args.Length];
					bool good = true;
					for (int i = 0; i < ctorArgs.Length; i++)
					{
						try
						{
							Type argType = ctorArgs[i].ParameterType;
							if (argType.IsEnum)
							{
								values[i] = Enum.Parse(argType, args[i], true);
							}
							else
							{
								values[i] = Convert.ChangeType(args[i], ctorArgs[i].ParameterType);
							}
						}
						catch (Exception ex)
						{
							if (ex is ArgumentException || ex is ArgumentNullException || ex is InvalidCastException)
							{
								good = false;
								break;
							}
							throw;
						}
					}

					if (good)
					{
						object value = ci.Invoke(values);
						pi.SetValue(control, value);
						break;
					}
				}
			}
		}
	}
}
