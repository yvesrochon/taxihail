using System.Collections.Generic;
using System.Linq;
using apcurium.Framework.Extensions;

namespace TaxiMobileApp
{
	public class Bootstrapper
	{
		
		private IModule[] _modules;
		
		public Bootstrapper (IModule[] modules)
		{			
			
			var list = new List<IModule>();
			if ( modules.Count() > 0 )
			{
				list.AddRange( modules );
			}
			list.Add( new ModuleService() );
			 _modules = list.ToArray();
		}
		
		public void Run()
		{												
			_modules.ForEach(  m => m.Initialize() );
			
		}
	}
}

