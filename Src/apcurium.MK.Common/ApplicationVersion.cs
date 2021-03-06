﻿using System;
using System.Linq;
using System.Text;

namespace apcurium.MK.Common
{
	public struct ApplicationVersion : IComparable, IComparable<ApplicationVersion>, IEquatable<ApplicationVersion>
	{
	    readonly ushort[] _versionNumbers;

		public ApplicationVersion(string version)
		{
			if (version != null)
			{
				_versionNumbers = version.Split('.').Select(
					(string c) =>
					{
						ushort v;
						if (ushort.TryParse(c, out v))
						{
							return v;
						}
						return (ushort)0;
					}
					).ToArray();
			}
			else
			{
				_versionNumbers = new ushort[0];
			}
		}

		public ushort[] GetVersionNumbers()
		{
			return _versionNumbers;
		}

		public static int Compare(ApplicationVersion v1, ApplicationVersion v2)
		{
			int result = 0;

			ushort[] av1 = v1.GetVersionNumbers();
			ushort[] av2 = v2.GetVersionNumbers();

			for (int i = 0; i < Math.Min(av1.Length, av2.Length); i++)
			{
				if (av1[i] > av2[i])
				{
					result = 1;
					break;
				}
				else if (av1[i] < av2[i])
				{
					result = -1;
					break;
				}
			}

			if (result == 0 && av1.Length != av2.Length)
			{
				ushort[] av3 = (av1.Length > av2.Length ? av1 : av2);
				ushort[] av4 = (av1.Length > av2.Length ? av2 : av1);

				for (int i = av4.Length; i < av3.Length; i++)
				{
					if (av3[i] > 0)
					{
						result = av1.Length > av2.Length ? 1 : -1;
						break;
					}
				}
			}

			return result;
		}

		public static bool operator >(ApplicationVersion v1, ApplicationVersion v2)
		{
			return Compare(v1, v2) == 1;
		}

		public static bool operator <(ApplicationVersion v1, ApplicationVersion v2)
		{
			return Compare(v1, v2) == -1;
		}

		public static bool operator ==(ApplicationVersion v1, ApplicationVersion v2)
		{
			return Compare(v1, v2) == 0;
		}

		public static bool operator !=(ApplicationVersion v1, ApplicationVersion v2)
		{
			return Compare(v1, v2) != 0;
		}

		public static bool operator >=(ApplicationVersion v1, ApplicationVersion v2)
		{
			int c = Compare(v1, v2);
			return (c == 0 || c == 1);
		}
		public static bool operator <=(ApplicationVersion v1, ApplicationVersion v2)
		{
			int c = Compare(v1, v2);
			return (c == 0 || c == -1);
		}

		public int CompareTo(ApplicationVersion v1)
		{
			return Compare(this, v1);
		}

		public bool Equals(ApplicationVersion v1)
		{
			return Compare(this, v1) == 0;
		}

		public int CompareTo(object obj)
		{
			if (obj != null && obj.GetType() == typeof(ApplicationVersion))
			{
				Compare(this, (ApplicationVersion)obj);
			}

			return 1;
		}

		public override bool Equals(object obj)
		{
			return CompareTo(obj) == 0;
		}

		public override string ToString()
		{
			StringBuilder appv = new StringBuilder();

			if (_versionNumbers != null)
			{
				for (int i = 0; i < _versionNumbers.Length; i++)
				{
					appv.Append(_versionNumbers[i].ToString());

					if (i < _versionNumbers.Length - 1)
					{
						appv.Append('.');
					}
				}
			}

			return appv.ToString();
		}
	}
}