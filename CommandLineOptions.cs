using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SmartCommandLineParser
{
    public class CommandLineOptions
    {
        List<Option> Options = new List<Option>();
        List<string> Args;
        int ParamWoSwitchIndex;
        Dictionary<string, object> Parameters;
	
		public CommandLineOptions() {}

        public T Get<T>(string name)
        {
            if (!Parameters.ContainsKey(name)) throw new KeyNotFoundException("Option '" + name + "' is not defined.");
            return (T)Parameters[name];
        }

        public void AddRequired<T>(string name, IEnumerable<string> switches = null, string help = "")
        {
            AddInner<T>(name, default(T), switches, help, repeatable: false, required: true);
        }

        public void AddRequired<T>(string name, string @switch, string help = "")
        {
            AddRequired<T>(name, new[] { @switch }, help);
        }

        public void AddOptional<T>(string name, T defaultValue, IEnumerable<string> switches = null, string help = "")
        {
            AddInner<T>(name, defaultValue, switches, help, repeatable: false, required: false);
        }

        public void AddOptional<T>(string name, T defaultValue, string @switch, string help = "")
        {
            AddOptional<T>(name, defaultValue, new[] { @switch }, help);
        }

        public void AddRepeatable<T>(string name, IEnumerable<string> switches = null, string help = "")
        {
            AddInner<T>(name, new List<T>(), switches, help, repeatable: true, required: false);
        }

        public void AddRepeatable<T>(string name, string @switch, string help = "")
        {
            AddRepeatable<T>(name, new[] { @switch }, help);
        }

        void AddInner<T>(string name, object defaultValue, IEnumerable<string> switches, string help, bool repeatable, bool required)
        {
            if (!HasOption(name))
            {
                Options.Add(new Option {
                    Name = name,
                    DefaultValue = defaultValue,
                    Type = typeof(T),
                    Switches = switches,
                    Help = help,
                    Repeatable = repeatable,
                    Required = required,
                });
            }
            else
            {
                throw new CommandLineParserException("Option '" + name + "' already added.");
            }
        }

        public string GetHelpMessage(string prefix = "\t")
		{
			var maxSwitchLength = 0;
			foreach (var opt in Options)
			{
				if (opt.Switches != null && opt.Switches.Any())
				{
					maxSwitchLength = Math.Max(maxSwitchLength, string.Join(", ", opt.Switches).Length);
				}
				else
				{
					maxSwitchLength = Math.Max(maxSwitchLength, opt.Name.Length + 2);
				}
			}

            var s = "";
            foreach (var opt in Options)
			{
				if (opt.Switches != null && opt.Switches.Any())
				{
					s += prefix + string.Join(", ", opt.Switches).PadRight(maxSwitchLength + 1, ' ');
				}
				else
				{
					s += prefix + ("<" + opt.Name + ">").PadRight(maxSwitchLength + 1, ' ');
				}
				
				if (opt.Help != null && opt.Help != "") 
				{
					var helpLines = opt.Help.Split('\n');
                    s += helpLines.First() + "\n";
					s += string.Join("", helpLines.Skip(1).Select(x => prefix + "".PadLeft(maxSwitchLength + 1, ' ') + x + "\n"));
				}
				else
				{
					s += "\n";
				}
				
				s += "\n";
			}

            return s.TrimEnd() + "\n";
		}
		
		public void Parse(IEnumerable<string> args)
        {
            this.Args = new List<string>(args);
            ParamWoSwitchIndex = 0;
			
	        Parameters = new Dictionary<string, object>();
            foreach (var opt in Options.Where(x => !x.Required))
            {
			    Parameters[opt.Name] = opt.DefaultValue;
            }

            while (this.Args.Count > 0)
            {
                ParseElement();
            }

            foreach (var option in Options)
            {
                if (option.Required && !Parameters.ContainsKey(option.Name))
                {
                    throw new CommandLineParserException("Required " + (option.Switches != null ? "option " + string.Join(", ", option.Switches) : "<" + option.Name + ">") + " is not specified.");
                }
            }
        }

        void ParseElement()
        {
            var arg = Args[0];
            Args.RemoveAt(0);

            if (arg != "--")
            {
                if (arg.StartsWith("-") && arg != "-")
                {
                    arg = Regex.Replace(arg, "^(--?.+)=(.+)$", re => {
                        Args.Insert(0, re.Groups[2].Value);
                        return re.Groups[1].Value;
                    });

                    foreach (var opt in Options)
                    {
                        if (opt.Switches != null)
                        {
                            foreach (var s in opt.Switches)
                            {
                                if (s == arg)
                                {
                                    ParseValue(opt, arg);
                                    return;
                                }
                            }
                        }
                    }

                    throw new CommandLineParserException("Unknow switch '" + arg + "'.");
                }
                else
                {
                    Args.Insert(0, arg);
                    ParseValue(GetNextNoSwitchOption(), Args[0]);
                }
            }
            else
            {
                while (Args.Count > 0) ParseValue(GetNextNoSwitchOption(), Args[0]);
            }
        }

        void ParseValue(Option opt, string s)
        {
            if (opt.Type == typeof(int))
            {
                EnsureValueExist(s);
                var v = int.Parse(Args[0]);
                Args.RemoveAt(0);
                if (!opt.Repeatable) Parameters[opt.Name] = v;
                else AddRepeatableValue(opt.Name, v);
            }
            else
            if (opt.Type == typeof(double))
            {
                EnsureValueExist(s);
                var v = double.Parse(Args[0]);
                Args.RemoveAt(0);
                if (!opt.Repeatable) Parameters[opt.Name] = v;
                else AddRepeatableValue(opt.Name, v);
            }
            else
            if (opt.Type == typeof(bool))
            {
                Parameters[opt.Name] = !(bool)opt.DefaultValue;
            }
            else
            if (opt.Type == typeof(string))
            {
                EnsureValueExist(s);
                var v = Args[0];
                Args.RemoveAt(0);
                if (!opt.Repeatable) Parameters[opt.Name] = v;
                else AddRepeatableValue(opt.Name, v);
            }
            else
            if (opt.Type.IsEnum)
            {
                EnsureValueExist(s);
                var v = Enum.Parse(opt.Type, Args[0], true);
                Args.RemoveAt(0);
                if (!opt.Repeatable) Parameters[opt.Name] = v;
                else AddRepeatableValue(opt.Name, v);
            }
            else
            {
                throw new CommandLineParserException("Option type '" + opt.Type + "' not supported.");
            }
        }

        bool HasOption(string name)
        {
            return Options.Any(opt => opt.Name == name);
        }

        void EnsureValueExist(string s)
        {
            if (Args.Count == 0)
            {
                throw new CommandLineParserException("Missing value after '" + s + "' switch.");
            }
        }

        Option GetNextNoSwitchOption()
        {
            for (var i = ParamWoSwitchIndex; i < Options.Count; i++)
            {
                if (Options[i].Switches == null)
                {
                    if (!Options[i].Repeatable) ParamWoSwitchIndex = i + 1;
                    return Options[i];
                }
            }

            throw new CommandLineParserException("Unexpected argument '" + Args[0] + "'.");
        }

        void AddRepeatableValue<T>(string name, T value)
        {
            if (!Parameters.ContainsKey(name)) Parameters[name] = new List<T>();
			((List<T>)Parameters[name]).Add(value);
        }
	}
}